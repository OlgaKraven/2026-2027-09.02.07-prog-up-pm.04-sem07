using System.Text.Json;
using Shop;

bool source = args.Contains("--source");
string root = Path.GetFullPath(args.FirstOrDefault(x => x != "--source") ?? ".");
int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
StoreData Copy(StoreData d) => JsonSerializer.Deserialize<StoreData>(JsonSerializer.Serialize(d))!;
CartLine Line(Product p, string s, int q) => new() { ProductId=p.Id, Name=p.Name, Size=s, Quantity=q, Price=p.DiscountPrice };
string[] Ids(IEnumerable<Product> p) => p.Select(x=>x.Id).ToArray();
foreach (string file in Directory.GetFiles(Path.Combine(root,"stands","data"),"variant-*.json").Order())
{
    var data=JsonSerializer.Deserialize<StoreData>(File.ReadAllText(file))!;
    foreach(var p in data.Products) p.DiscountPrice=decimal.Round(p.Price,2);
    var p1=data.Products[0]; var p3=data.Products[2]; var size=p1.Stock.Keys.First();
    Check(data.Products.Count==5 && data.Users.Count==2 && data.Orders.Count==0,file+" baseline");
    Check((RepairLogic.Authenticate(data.Users,"nobody") is null)==!source,file+" D1 unknown");
    Check(RepairLogic.Authenticate(data.Users," client ")?.Login=="client",file+" known user");
    var draft=new List<CartLine>{Line(p1,size,1)};
    RepairLogic.Back(draft);
    Check(draft.Count==(source?0:1),file+" D2 return");
    var expected=data.Products.Where(p=>p.Category==p1.Category && (p.Name.Contains("сервисный",StringComparison.OrdinalIgnoreCase)||p.Description.Contains("сервисный",StringComparison.OrdinalIgnoreCase))).OrderBy(p=>p.DiscountPrice).ThenBy(p=>p.Id);
    var actual=RepairLogic.Catalog(data.Products," сервисный ",p1.Category,false);
    Check(Ids(expected).SequenceEqual(Ids(actual))==!source,file+" D3 joint filter");
    if(!source)
    {
        foreach(bool desc in new[]{false,true})
        {
            var want=desc?data.Products.OrderByDescending(p=>p.DiscountPrice).ThenBy(p=>p.Id):data.Products.OrderBy(p=>p.DiscountPrice).ThenBy(p=>p.Id);
            Check(Ids(want).SequenceEqual(Ids(RepairLogic.Catalog(data.Products,"","Все категории",desc))),file+" numeric order");
        }
        Check(RepairLogic.Catalog(data.Products,"несуществующийузел",p1.Category,false).Count==0,file+" empty result");
        Check(RepairLogic.Catalog(data.Products,"СервисКомплект","Все категории",false).Count==0,file+" manufacturer excluded");
        Check(Ids(RepairLogic.Catalog(data.Products,"СЕРВИСНЫЙ",p1.Category,false)).SequenceEqual(Ids(actual)),file+" case");
    }
    draft=new(){Line(p1,size,3)};
    foreach(var invalid in new[]{"0","-1","abc","1,5","999999999999999999999","3"})
    {
        Check(OrderService.ValidateQuantity(p1,size,invalid,draft)!="",file+" ready rejection "+invalid);
        Check(draft.Count==1 && draft[0].Quantity==3 && p1.Stock[size]==5,file+" rejection preserves state");
    }
    Check(OrderService.ValidateQuantity(p1,size,"2",draft)=="",file+" corrected input");
    var attempt=Copy(data);
    var snapshot=JsonSerializer.Serialize(attempt);
    var invalidLines=new List<CartLine>{Line(p1,size,1),Line(p3,p3.Stock.Keys.First(),999)};
    Check(OrderService.Commit(attempt,data.Users[0],invalidLines,new DateTime(2026,11,3))!="",file+" reject entire order");
    Check(snapshot==JsonSerializer.Serialize(attempt) && invalidLines.Count==2,file+" no partial update");
    attempt=Copy(data);
    var lines=new List<CartLine>{Line(p1,size,2),Line(p3,p3.Stock.Keys.First(),1)};
    Check(OrderService.Commit(attempt,data.Users[0],lines,new DateTime(2026,11,3))=="",file+" ready commit");
    Check(attempt.Products[0].Stock[size]==3 && attempt.Products[2].Stock[p3.Stock.Keys.First()]==p3.Stock.Values.First()-1,file+" both resources");
    var restart=Copy(attempt);
    Check(restart.Orders.Count==1 && restart.Orders[0].Lines.Count==2 && restart.Orders[0].Login=="client" && restart.Orders[0].Date==new DateTime(2026,11,3),file+" persistence roundtrip");
}
if(!source)
{
    var adapted=new[]{new Sensor("S2","Измеритель","диагностика","Датчики",20),new Sensor("S1","Измеритель","диагностика","Датчики",100)};
    Check(RepairLogic.Catalog(adapted,"диагностика","Датчики",false).First().Id=="S2","reuse on unrelated model without rewriting");
}
Console.WriteLine($"PASS {(source?"source: 3 defects reproduced, ready operation preserved":"fixed: diagnostics, rejection/recovery, order, adapter")} — {checks} assertions, 30 variants");
record Sensor(string Id,string Name,string Description,string Category,decimal DiscountPrice):ICatalogItem;
