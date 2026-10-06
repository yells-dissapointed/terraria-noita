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
function GameGetFrameNum() return 0 end
function SetRandomSeed() error('Noita RNG is not implemented; shuffle is disabled') end
function Random() error('This spell requires the unimplemented Noita RNG') end
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
        if request.shuffle then error('Shuffle requires native-compatible RNG') end
        for _, slot in ipairs(request.slots) do
            if not all_actions[slot.id] then error('Unknown Noita spell: ' .. slot.id) end
        end
        ConfigGun_ReadToLua(request.actions_per_round, false, request.reload_time, #request.slots)
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
