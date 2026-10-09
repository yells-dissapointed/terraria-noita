-- Our host adapter. Original Noita scripts are supplied by the user's installation.
-- Records completed cast trees; it does not implement Noita's native physics.
local source_files = assert(noita_sources)
local compiler, protected_call = loadstring, pcall
local set_hook = debug.sethook
-- Count hooks must also cover hot loops, which LuaJIT can otherwise compile.
if jit then jit.off() end
local loaded = {}
local maximum_nodes = 256
local instruction_budget = 1000000
local remaining = instruction_budget
local node_count = 0
local contexts, projectiles, events = {}, {}, {}
local all_actions = {}
local null_marker = {}

function dofile(path)
    local source = source_files[path]
    if not source then error('Unavailable Noita source: ' .. tostring(path)) end
    local fn, message = compiler(source, '@' .. path)
    if not fn then error(message) end
    return fn()
end
function dofile_once(path)
    if not loaded[path] then
        loaded[path] = true
        return dofile(path)
    end
end

local function copy(t)
    local result = {}
    for key, value in pairs(t) do
        if type(value) ~= 'function' then result[key] = value end
    end
    return result
end
local function record(kind, value)
    events[#events + 1] = { kind = kind, value = value == nil and null_marker or value }
end
local function context()
    return assert(contexts[#contexts], 'No active shot context')
end
function BeginProjectile(path)
    node_count = node_count + 1
    if node_count > maximum_nodes then error('Cast exceeds projectile limit') end
    local node = { entity = path, triggers = {} }
    local shot = context()
    shot.projectiles[#shot.projectiles + 1] = node
    projectiles[#projectiles + 1] = node
end
function EndProjectile()
    if #projectiles == 0 then error('Unbalanced EndProjectile') end
    projectiles[#projectiles] = nil
end
local function begin_trigger(kind, frames)
    local projectile = assert(projectiles[#projectiles], 'Trigger without carrier')
    if #contexts >= 16 then error('Cast exceeds trigger depth') end
    local payload = { projectiles = {} }
    local trigger = { kind = kind, delay_frames = frames or 0, payload = payload }
    projectile.triggers[#projectile.triggers + 1] = trigger
    contexts[#contexts + 1] = payload
end
function BeginTriggerHitWorld() begin_trigger('hit_world') end
function BeginTriggerDeath() begin_trigger('death') end
function BeginTriggerTimer(frames) begin_trigger('timer', frames) end
function EndTrigger()
    if #contexts <= 1 then error('Unbalanced EndTrigger') end
    contexts[#contexts] = nil
end
function RegisterGunAction(...)
    -- Noita's generated bridge is invoked only after the draw for this context.
    context().config = copy(c)
end
function SetProjectileConfigs()
    local shot = context()
    if not shot.config then error('Commit without configuration') end
    shot.committed = true
end
function RegisterGunShotEffects(...) record('shot_effects', { ... }) end
function StartReload(frames) record('reload_request', frames) end
function OnActionPlayed(id)
    record('action', id)
    record('action_mana', { id = id, mana = mana })
end
function OnNotEnoughManaForAction() record('insufficient_mana', true) end
function ActionUsed(id) record('action_used', id) end
function ActionUsesRemainingChanged(id, uses)
    record('uses_changed', { id = id, uses = uses })
    return true
end
function LogAction(name) record('log', name) end
function Reflection_RegisterProjectile(path) record('reflection', path) end
function print_error(message) error(message) end
-- Deterministic host PRNG, intentionally not bit-identical to Noita's native RNG.
local host = { frame = 0, x = 0, y = 0, hp = 4, max_hp = 4, money = 0, black_holes = 0, enemies = {}, projectiles = {}, wands = {} }
local globals, rng = {}, 1
function GameGetFrameNum() return host.frame end
function SetRandomSeed(x, y)
    rng = (math.floor(math.abs(x or 0)) * 16807 + math.floor(math.abs(y or 0)) * 48271) % 2147483646 + 1
end
function Random(a, b)
    rng = (rng * 16807) % 2147483647
    if a == nil then return rng / 2147483647 end
    if b == nil then b, a = a, 0 end
    return math.floor(a + (b - a + 1) * rng / 2147483647)
end
function Randomf(a, b) return (a or 0) + ((b or 1) - (a or 0)) * Random() end
function HasFlagPersistent() return true end -- This development catalog unlocks every imported card.
function GlobalsGetValue(key, fallback) return globals[key] or fallback end
function GlobalsSetValue(key, value) globals[key] = tostring(value) end
function GetUpdatedEntityID() return 1 end
function EntityGetTransform() return host.x, host.y end
function EntityGetWithTag(tag)
    if tag == 'player_unit' then return { 1 } end
    if tag == 'black_hole_giga' then local list = {}; for i = 1, host.black_holes do list[i] = 50000 + i end; return list end
    error('Host entity tag not supported: ' .. tag)
end
function EntityGetInRadiusWithTag(x, y, radius, tag)
    local source = tag == 'homing_target' and host.enemies or tag == 'projectile' and host.projectiles
    if not source then error('Host radius query not supported: ' .. tag) end
    local list = {}
    for i, point in ipairs(source) do if (point.x-x)^2 + (point.y-y)^2 <= radius^2 then list[#list+1] = 60000 + i end end
    return list
end
function EntityGetAllChildren(id)
    id = tonumber(id)
    if id == 1 then return { 2 } end
    if id == 2 then local list = { 3 }; for i in ipairs(host.wands) do list[#list+1] = 100+i end; return list end
    if id and id > 100 and id < 1000 then
        local list = {}; for j in ipairs(host.wands[id-100] or {}) do list[j] = id * 1000 + j end; return list
    end
    return {}
end
function EntityGetName(id) return tonumber(id) == 2 and 'inventory_quick' or '' end
function EntityHasTag(id, tag) id = tonumber(id); return tag == 'wand' and (id == 3 or id and id > 100 and id < 1000) end
function EntityGetFirstComponent(id, kind)
    id = tonumber(id)
    if id == 1 and (kind == 'DamageModelComponent' or kind == 'WalletComponent' or kind == 'Inventory2Component' or kind == 'InventoryGuiComponent' or kind == 'PlatformShooterPlayerComponent') then return { id = id, kind = kind } end
    if EntityHasTag(id, 'wand') and (kind == 'AbilityComponent' or kind == 'ItemComponent') then return { id = id, kind = kind } end
    if id and id >= 101001 and kind == 'ItemActionComponent' then return { id = id, kind = kind } end
    return nil
end
EntityGetFirstComponentIncludingDisabled = EntityGetFirstComponent
function EntityGetComponent(id, kind) local component = EntityGetFirstComponent(id, kind); return component and { component } or nil end
EntityGetComponentIncludingDisabled = EntityGetComponent
function ComponentGetValue2(comp, field)
    if comp.kind == 'DamageModelComponent' and (field == 'hp' or field == 'max_hp') then return host[field] end
    if comp.kind == 'WalletComponent' then if field == 'money' then return host.money end; if field == 'money_spent' then return 0 end end
    if comp.kind == 'Inventory2Component' and field == 'mActiveItem' then return 3 end
    if comp.kind == 'ItemActionComponent' and field == 'action_id' then return (host.wands[math.floor(comp.id/1000)-100] or {})[comp.id%1000] end
    error('Host component read not supported: ' .. comp.kind .. '.' .. field)
end
function ComponentGetValue(comp, field) return tostring(ComponentGetValue2(comp, field)) end
function ComponentSetValue2(comp, field, value)
    if comp.kind == 'DamageModelComponent' and field == 'hp' then host.hp = value; record('host_hp', value); return end
    if comp.kind == 'WalletComponent' and field == 'money' then local spent = host.money-value; host.money = value; record('host_money_spent', spent); return end
    if comp.kind == 'WalletComponent' and field == 'money_spent' then return end
    if comp.kind == 'PlatformShooterPlayerComponent' and field == 'mCessationLifetime' then record('host_cessation', value); return end
    if comp.kind == 'PlatformShooterPlayerComponent' and field == 'mCessationDo' then return end
    if comp.kind == 'AbilityComponent' and (field == 'mNextFrameUsable' or field == 'mCastDelayStartFrame') then record('host_ability_timing', { field = field, value = value }); return end
    if comp.kind == 'InventoryGuiComponent' and field == 'mDisplayFireRateWaitBar' then return end
    error('Host component write not supported: ' .. comp.kind .. '.' .. field)
end
ComponentSetValue = ComponentSetValue2
function EntityInflictDamage(id, damage)
    if id ~= 1 then error('Only caster resource damage is supported') end
    host.hp = math.max(0.04, host.hp - damage); record('host_hp', host.hp)
end
function EntityLoad(path) error('Entity script loader not ported: ' .. path) end
function bridge_context(value) host = value end
function BaabInstruction() error('BAAB native instructions are not implemented') end

-- Restrict file/process APIs. Native callbacks never longjmp through managed code.
io, os, package, require, debug, loadfile = nil, nil, nil, nil, nil, nil
noita_sources = nil
dofile('data/scripts/gun/gun.lua')
for _, action in ipairs(actions) do all_actions[action.id] = action end

local function guarded(fn)
    remaining = instruction_budget
    set_hook(function()
        remaining = remaining - 1000
        if remaining <= 0 then error('Cast exceeds Lua instruction budget') end
    end, '', 1000)
    local ok, result = protected_call(fn)
    set_hook()
    if not ok then error(result) end
    return result
end

function bridge_configure(request)
    return guarded(function()
        for _, slot in ipairs(request.slots) do
            if not all_actions[slot.id] then error('Unknown Noita spell: ' .. slot.id) end
        end
        ConfigGun_ReadToLua(request.actions_per_round, request.shuffle, request.reload_time, #request.slots)
        _set_gun()
        local state = {}
        ConfigGunActionInfo_Init(state)
        state.fire_rate_wait = request.cast_delay
        __globaldata = state
        _set_gun2()
        _clear_deck(false)
        for index, slot in ipairs(request.slots) do
            _add_card_to_deck(slot.id, index, slot.uses_remaining, slot.identified)
        end
    end)
end

-- Minimal deterministic JSON serializer. Empty arrays are marked explicitly below.
local array_marker = {}
local function array(t) return setmetatable(t, array_marker) end
local function quote(text)
    return '"' .. text:gsub('[%z\1-\31\\"]', function(ch)
        if ch == '"' then return '\\"' end
        if ch == '\\' then return '\\\\' end
        return string.format('\\u%04x', string.byte(ch))
    end) .. '"'
end
local function json(value)
    if value == null_marker then return 'null' end
    local kind = type(value)
    if kind == 'nil' then return 'null' end
    if kind == 'boolean' then return value and 'true' or 'false' end
    if kind == 'number' then
        if value ~= value or value == math.huge or value == -math.huge then error('Nonfinite output') end
        return string.format('%.17g', value)
    end
    if kind == 'string' then return quote(value) end
    if kind ~= 'table' then error('Unserializable host output: ' .. kind) end
    local parts = {}
    if getmetatable(value) == array_marker then
        for index = 1, #value do parts[#parts + 1] = json(value[index]) end
        return '[' .. table.concat(parts, ',') .. ']'
    end
    local keys = {}
    for key in pairs(value) do keys[#keys + 1] = tostring(key) end
    table.sort(keys)
    for _, key in ipairs(keys) do parts[#parts + 1] = quote(key) .. ':' .. json(value[key]) end
    return '{' .. table.concat(parts, ',') .. '}'
end
local function mark_shot(shot)
    array(shot.projectiles)
    for _, node in ipairs(shot.projectiles) do
        array(node.triggers)
        for _, trigger in ipairs(node.triggers) do mark_shot(trigger.payload) end
    end
end
function bridge_cast(request)
    return guarded(function()
        local root = { projectiles = {} }
        contexts, projectiles, events = { root }, {}, {}
        node_count = 0
        _start_shot(request.mana)
        for _, id in ipairs(request.permanent) do
            if not all_actions[id] then error('Unknown permanent spell: ' .. id) end
            _play_permanent_card(id)
        end
        _draw_actions_for_shot(true)
        if #contexts ~= 1 or #projectiles ~= 0 then error('Unbalanced cast structure') end
        mark_shot(root)
        array(events)
        for _, event in ipairs(events) do
            if event.kind == 'shot_effects' then array(event.value) end
        end
        local remaining_deck = array({})
        for _, card in ipairs(deck) do remaining_deck[#remaining_deck + 1] = card.id end
        local discard = array({})
        for _, card in ipairs(discarded) do discard[#discard + 1] = card.id end
        return json({ version = 1, mana = mana, root = root, events = events,
                      deck = remaining_deck, discarded = discard })
    end)
end
function bridge_version() return _VERSION end
function bridge_defaults()
    local defaults = {}
    ConfigGunActionInfo_Init(defaults)
    return json(defaults)
end
function bridge_catalog()
    local cards = array({})
    for _, action in ipairs(actions) do
        cards[#cards + 1] = { Id = action.id, Sprite = action.sprite or '', Type = action.type or -1 }
    end
    return json(cards)
end
