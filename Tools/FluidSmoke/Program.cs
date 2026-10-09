using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using terrarianoita.Core;

if (args.Length != 2) { Console.Error.WriteLine("FluidSmoke <Noita extracted root> <report.json>"); return 2; }
var catalog = new NoitaMaterialCatalog(args[0]); var chemistry = new MaterialChemistry(catalog);
var checks = new List<string>();
void Check(bool ok,string message) { if(!ok)throw new Exception(message);checks.Add(message); }
bool Basin(int x,int y) => x<0||x>=12||y<0||y>=8;
bool Column(int x,int y) => x!=0||y<0||y>=2;
void Run(MaterialGrid grid, Func<int,int,bool> walls, int frames=80) { for(int f=0;f<frames;f++)grid.Step(walls,catalog.Get,512,f); }
Dictionary<string,long> Totals(MaterialGrid grid) => grid.Cells.GroupBy(c=>c.Material).ToDictionary(g=>g.Key,g=>g.Sum(c=>(long)c.Amount));
bool Same(Dictionary<string,long> a,Dictionary<string,long> b) => a.Count==b.Count&&a.All(p=>b.GetValueOrDefault(p.Key)==p.Value);
Check(catalog.ImportedCount==466&&catalog.Materials.ContainsKey("noita_diluted_acid"),"original materials plus explicitly named adapted dilute acid");
Check(catalog.Get("water").Density>catalog.Get("oil").Density,"imported density puts water below oil");
var partial=new MaterialGrid();partial.Add(0,1,"oil",50,Column);partial.Add(0,0,"water",100,Column);Run(partial,Column);
Check(partial.Count==1&&partial.AmountOf(0,1,"oil")==50&&partial.AmountOf(0,1,"water")==100,"partial different liquids fill the same bottom cell without a vertical gap");
Check(partial.Add(0,1,"blood",200,Column)==105&&partial.AmountAt(0,1)==255,"shared capacity caps the whole mixture, not each ingredient");
var sample=new FlaskContents();
Check(partial.Scoop(0,1,sample,80,n=>catalog.Get(n).Scoopable)==80&&sample.Materials.Count==3&&partial.Volume+sample.Total==255,"a scoop transfers a sample of every liquid layer and conserves contents");
sample.Add("water",9999);long left=partial.Volume;
Check(partial.Scoop(0,1,sample,80,n=>catalog.Get(n).Scoopable)==0&&partial.Volume==left,"a full flask leaves all mixed pool contents untouched");
var inverted=new MaterialGrid();inverted.Add(0,0,"water",255,Column);inverted.Add(0,1,"oil",255,Column);Run(inverted,Column);
Check(inverted.AmountOf(0,1,"water")==255&&inverted.AmountOf(0,0,"oil")==255&&inverted.Volume==510,"inverted water/oil layers exchange equal volumes until density sorted");
var gas=new MaterialGrid();gas.Add(0,0,"water",255,Column);gas.Add(0,1,"steam",255,Column);Run(gas,Column);
Check(gas.AmountOf(0,0,"steam")==255&&gas.AmountOf(0,1,"water")==255,"gas rises through a liquid-filled cell without deleting either phase");
var pressure=new MaterialGrid();pressure.Add(0,1,"oil",255,Column);
Check(pressure.ImportNative(0,1,"water",200,Column)==200&&pressure.Volume==455,"native incursion is retained rather than discarded at a full custom cell");
Run(pressure,Column);
Check(pressure.Volume==455&&pressure.Positions.All(p=>pressure.AmountAt(p.X,p.Y)<=255),"stored pressure spills into available space without overfilling ordinary cells");
var released=new MaterialGrid();released.Add(0,1,"water",40,Column);released.Take(0,1,40,"water");int contacts=0;
released.Step(Column,catalog.Get,16,0,(_,_)=>contacts++);
Check(contacts==0&&released.Count==0,"stale flow wakes cannot re-import fluid already returned to Terraria");
var mixed=new MaterialGrid();
for(int x=2;x<10;x++){mixed.Add(x,0,x%2==0?"oil":"water",170,Basin);mixed.Add(x,0,"blood",70,Basin);}
var before=Totals(mixed);bool conserved=true,bounded=true;
for(int f=0;f<600;f++){mixed.Step(Basin,catalog.Get,128,f);conserved&=Same(before,Totals(mixed));bounded&=mixed.Positions.All(p=>mixed.AmountAt(p.X,p.Y)<=255&&!Basin(p.X,p.Y));}
Check(conserved&&bounded,"600 mixed-fluid frames preserve every material and shared cell capacity");
var restored=new MaterialGrid();foreach(var c in mixed.Cells)restored.Add(c.X,c.Y,c.Material,c.Amount,Basin,allowPressure:true);
Check(Same(Totals(restored),before)&&restored.Cells.OrderBy(c=>c.X).ThenBy(c=>c.Y).ThenBy(c=>c.Material).SequenceEqual(mixed.Cells.OrderBy(c=>c.X).ThenBy(c=>c.Y).ThenBy(c=>c.Material)),"flattened save records recreate all mixed layers exactly");
var radioactive=new MaterialGrid();radioactive.Add(0,1,"water",60,Column);radioactive.Add(0,1,"radioactive_liquid",60,Column);
var purification=chemistry.Available("water","radioactive_liquid").First(r=>!r.Adapted&&r.OutputA=="water"&&r.OutputB=="water");
chemistry.Apply(radioactive,new(0,1,"water",60),new(0,1,"radioactive_liquid",60),purification,10);
Check(radioactive.AmountOf(0,1,"water")==70&&radioactive.AmountOf(0,1,"radioactive_liquid")==50&&radioactive.Volume==120,"original water/radioactive recipe changes only the reacted aliquot");
Check(chemistry.Available("radioactive_liquid","water").Any(r=>r.Id==purification.Id&&r.OutputA=="water"&&r.OutputB=="water"),"binary recipes work in both contact orientations");
var mana=chemistry.Available("water","magic_liquid_mana_regeneration");
Check(mana.Any(r=>!r.Adapted&&r.OutputA=="magic_liquid_mana_regeneration"&&r.OutputB==r.OutputA),"original mana potion/water reaction is imported");
Check(chemistry.Available("blood","poison").Any(r=>!r.Adapted&&r.OutputA=="slime"&&r.OutputB=="smoke"),"original blood/poison reaction keeps both products");
Check(chemistry.Available("steam","rock_static").Any(r=>!r.Adapted&&r.OutputA=="water"&&r.OutputB=="rock_static"),"original wall-catalysed steam condensation is supported");
var dilute=chemistry.Available("acid","water").First(r=>r.Adapted);
Check(dilute.OutputA=="noita_diluted_acid"&&dilute.OutputB==dilute.OutputA&&!catalog.Get(dilute.OutputA).Has("acid"),"adapted acid dilution produces a weaker non-corroding liquid");
Check(new MaterialChemistry(catalog,false).Available("acid","water").All(r=>!r.Adapted),"adapted reactions can be disabled independently of imported chemistry");
var originalOnly=new MaterialChemistry(catalog,false);
Check(originalOnly.Available("water","radioactive_liquid").Any(),"imported chemistry stays enabled when adapted recipes are off");
Check(chemistry.Definitions.Length==325&&chemistry.Definitions.Any(r=>r.Deferred.Contains("input_cell3"))&&chemistry.Definitions.Any(r=>r.Deferred.Contains("blob_radius")),"advanced original recipe requirements are retained as deferred, not silently ignored");
var burn=new MaterialGrid();burn.Add(0,1,"water",80,Column);burn.Add(0,1,"fire",80,Column);
var extinction=chemistry.Available("water","fire").First(r=>r.Adapted);
chemistry.Apply(burn,new(0,1,"water",80),new(0,1,"fire",80),extinction,16);
Check(burn.AmountOf(0,1,"steam")==16&&burn.AmountOf(0,1,"fire")==64&&burn.Volume==144,"extinguishing removes only the explicitly air-converted volume");
var capped=new MaterialGrid();for(int i=0;i<MaterialGrid.MaximumMaterialsPerCell;i++)capped.Add(0,1,"fixture"+i,10,Column);
Check(capped.Transform(new(0,1,"fixture0",10),"newProduct",5)==0&&capped.Volume==160&&capped.AmountOf(0,1,"fixture0")==10,"product-slot exhaustion rolls back atomically");
var chemistryGrid=new MaterialGrid();chemistryGrid.Add(0,1,"water",120,Column);chemistryGrid.Add(0,1,"radioactive_liquid",120,Column);
for(int f=0;f<3600;f+=15)chemistry.Step(chemistryGrid,Column,f,2);
Check(chemistryGrid.AmountOf(0,1,"radioactive_liquid")==0&&chemistryGrid.Volume==240,"the scheduled contact loop actually purifies a mixed pool");
var cases = new[]{("water","radioactive_liquid"),("water","magic_liquid_mana_regeneration"),("water","magic_liquid_invisibility"),("blood","poison"),("acid","water"),("water","fire"),("oil","fire"),("steam","rock_static")};
var ingredients=catalog.Materials.Values.Where(MaterialGrid.Mobile).Select(m=>m.Name).Append("air").Append("rock_static").Distinct().ToArray();
var supportedIds=new HashSet<string>();
for(int i=0;i<ingredients.Length;i++)for(int j=i;j<ingredients.Length;j++)
    foreach(var r in chemistry.Available(ingredients[i],ingredients[j]).Where(r=>!r.Adapted))
        if((ingredients[i] is not ("air" or "rock_static")||r.OutputA==ingredients[i])&&(ingredients[j] is not ("air" or "rock_static")||r.OutputB==ingredients[j]))supportedIds.Add(r.Id);
File.WriteAllText(args[1],JsonSerializer.Serialize(new{version=BuildStamp.Version,passed=checks.Count,checks,imported_materials=catalog.ImportedCount,adapted_materials=catalog.Materials.Count-catalog.ImportedCount,
    reaction_definitions=chemistry.ImportedDefinitions,binary_candidates=chemistry.EligibleDefinitions,
    supported_xml_recipes=supportedIds.Count,supported_recipe_ids=supportedIds.OrderBy(s=>int.Parse(s[4..])),
    note="Candidates still require supported material/product matching. These are tile-volume adapters, not pixel-accurate Noita rates. Blob, directional, timed and three-input recipes remain deferred.",
    examples=cases.Select(p=>new{a=p.Item1,b=p.Item2,outcomes=chemistry.Available(p.Item1,p.Item2)}),definitions=chemistry.Definitions},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS: {checks.Count} fluid checks; {chemistry.ImportedDefinitions} original recipes, {chemistry.EligibleDefinitions} binary candidates, {supportedIds.Count} with supported material/output matches");
return 0;
