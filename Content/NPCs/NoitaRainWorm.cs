#nullable enable
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using terrarianoita.Common;
using terrarianoita.Core;

namespace terrarianoita.Content.NPCs;

public sealed class NoitaRainWorm : ModNPC
{
    public override string Texture => "Terraria/Images/NPC_" + NPCID.GiantWormHead;
    private Vector2[] points=Array.Empty<Vector2>();
    private NoitaSpriteAsset[] sprites=Array.Empty<NoitaSpriteAsset>();
    private int age;
    private bool initialized;
    private readonly Projectile terrainProbe=new(){type=ProjectileID.Bomb};
    public override void SetDefaults()
    { NPC.width=NPC.height=24;NPC.lifeMax=3500;NPC.damage=35;NPC.defense=4;NPC.noGravity=true;NPC.noTileCollide=true;NPC.knockBackResist=0;NPC.aiStyle=-1;NPC.value=0; }
    public override bool CheckActive()=>false;
    public override void AI()
    {
        if(Main.netMode!=NetmodeID.SinglePlayer) {NPC.active=false;return;}
        if(!initialized)
        {
            initialized=true;
            try { var catalog=ModContent.GetInstance<AdapterSystem>().Assets.Catalog; var asset=catalog.Entity("data/entities/misc/worm_big_worm_rain.xml");
                sprites=asset.Definition.Children.Where(c=>c.Name=="SpriteComponent"&&c.Get("image_file").StartsWith("data/enemies_gfx/worm_")).Select(c=>catalog.Sprite(c.Get("image_file"),c)).ToArray(); }
            catch(Exception e){Mod.Logger.Warn("Worm sprite import: "+e.Message);}
            points=new Vector2[Math.Max(7,sprites.Length)];Array.Fill(points,NPC.Center);
        }
        age++; if(age>900) {NPC.active=false;return;}
        NPC.TargetClosest(false);var player=Main.player[NPC.target];
        Vector2 target=player.Center;
        var prey=Main.npc.Where(n=>n.active&&!n.friendly&&n.whoAmI!=NPC.whoAmI&&n.type!=Type&&Vector2.DistanceSquared(n.Center,NPC.Center)<256*256).OrderBy(n=>Vector2.DistanceSquared(n.Center,NPC.Center)).FirstOrDefault();
        if(prey!=null)target=prey.Center;
        float angle=NPC.velocity.LengthSquared()>.01f?NPC.velocity.ToRotation():MathHelper.PiOver2;
        float delta=MathHelper.WrapAngle((target-NPC.Center).ToRotation()-angle);
        NPC.velocity=(angle+Math.Clamp(delta,-.04f,.04f)).ToRotationVector2()*5;
        NPC.rotation=NPC.velocity.ToRotation();points[0]=NPC.Center;
        for(int i=1;i<points.Length;i++){var d=points[i]-points[i-1];points[i]=points[i-1]+d.SafeNormalize(-NPC.velocity.SafeNormalize(Vector2.UnitX))*16*SpellVisuals.Scale;}
        if(age%3==0){terrainProbe.Center=NPC.Center;SpellTerrain.Eat(terrainProbe,9);}
        if(age%15==0&&prey!=null&&NPC.Hitbox.Intersects(prey.Hitbox))prey.SimpleStrikeNPC(35,NPC.direction);
    }
    public override bool PreDraw(SpriteBatch spriteBatch,Vector2 screenPos,Color drawColor)
    {
        if(sprites.Length==0||points.Length==0)return true;
        var cache=ModContent.GetInstance<AdapterSystem>().Assets;
        for(int i=points.Length-1;i>=0;i--){int index=Math.Min(i,sprites.Length-1);float rotation=i==0?NPC.rotation:(points[i-1]-points[i]).ToRotation();cache.Draw(sprites[index],points[i],rotation,age,SpellVisuals.Scale,out _);}
        return false;
    }
}
