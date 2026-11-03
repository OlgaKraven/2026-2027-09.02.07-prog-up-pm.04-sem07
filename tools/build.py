"""Build the student site and downloads from the reviewed course source.

PDFs are exported from print-method.html and print-areas.html with Chromium.
Development instructions and acceptance evidence are kept outside the student pages.
"""
import hashlib, html, json, pathlib, shutil, zipfile, re

ROOT=pathlib.Path(__file__).resolve().parents[1]
SITE=ROOT/'site'
C=json.loads((ROOT/'content/course.json').read_text(encoding='utf-8'))
def esc(s): return html.escape(str(s),quote=True)
def put(p,s):
    p=SITE/p; p.parent.mkdir(parents=True,exist_ok=True); p.write_text(s,encoding='utf-8',newline='\n')
def ptext(s): return ''.join('<p>'+esc(p).replace('\n','<br>')+'</p>' for p in s.split('\n\n'))
def card(b,i):
    if b['kind']=='image':
        path=b['path'].removeprefix('site/')
        return '<li class="illustration"><figure><img src="'+esc(path)+'" alt="'+esc(b['text'])+'"><figcaption>'+esc(b['title'])+'</figcaption></figure></li>'
    text=b.get('text','').split('\n\nПроверка:')[0]
    inner=ptext(text)
    if b['kind']=='code': inner='<pre><code>'+esc(b['code'])+'</code></pre>'
    if b.get('check'): inner+='<p class="result"><strong>Проверка.</strong> '+esc(b['check'])+'</p>'
    return f'<li class="step"><span class="number">{i}</span><section class="card"><h3>{esc(b["title"])}</h3>{inner}</section></li>'
def head(kicker,title,sub=''):
    return f'<header class="chapter"><p class="kicker">{esc(kicker)}</p><h1>{esc(title)}</h1>'+ (f'<p>{esc(sub)}</p>' if sub else '')+'</header>'
def chapter(s,num,anchor=''):
    body=[];step=0
    for b in s['blocks']:
        if b['kind']!='image':step+=1
        body.append(card(b,step))
    return f'<article class="lesson" id="{esc(anchor)}">'+head(f'ПМ.04 · Раздел {num}',s['title'])+'<ol class="route">'+''.join(body)+'</ol></article>'
def nav(active):
    links=[('index.html','Задание'),('areas.html','Предметные области'),('lessons.html','Методические рекомендации')]
    return '<a class="skip" href="#main">К содержанию</a><nav aria-label="Разделы практики">'+''.join(f'<a href="{p}"'+(' aria-current="page"' if p==active else '')+'>'+n+'</a>' for p,n in links)+'</nav>'
def page(title,body,active='',extra=''):
    return '<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+esc(title)+' — УП ПМ.04</title><link rel="icon" href="assets/favicon.svg"><link rel="stylesheet" href="assets/style.css"></head><body>'+nav(active)+'<main id="main">'+body+'</main>'+extra+'</body></html>'
def buttons(items): return '<div class="actions">'+''.join(f'<a class="button" href="{esc(p)}">{esc(n)}</a>' for p,n in items)+'</div>'

files=section=dict(title='Файлы для работы',blocks=[
dict(kind='text',title='Исходный проект',text='Начальная копия с тремя дефектами и готовой операцией заявки. Архив сохранить отдельно от рабочих папок.',check='Открыть stands/Shop-source/Shop.csproj после распаковки.'),
dict(kind='text',title='Данные назначенного варианта',text='В карточке своей области скачать variant-NN.json. Из него создать baseline.json и work-data.json; до начала испытаний сверить Id, исполнения и дополнительные условия.',check='Демонстрационные данные не подменяют индивидуальную область.'),
dict(kind='text',title='Образец и формы',text='Исправленный проект «Контур» служит отдельной сверкой после анализа. Шаблон отчёта задаёт состав доказательств. Аттестационный лист, индивидуальное задание и презентацию предоставляет преподаватель через EatDoners.',check='Ссылка на собственный репозиторий указывается в самом отчёте.')])
filebuttons=buttons([('downloads/Shop-source.zip','Исходный проект'),('areas.html','Данные своего варианта'),('downloads/Shop-fixed-example.zip','Образец для сверки'),('downloads/report-template.md','Шаблон отчёта'),('downloads/contract.md','Контракт компонентов')])
assignment=''.join(chapter(s,1,'assignment') for s in C['assignment'])
material=chapter(files,2,'files')+filebuttons
method=''.join(chapter(s,f'3.{i}',f'step-{i}') for i,s in enumerate(C['guide'],1))
biblio=[('Обзор Windows Presentation Foundation','https://learn.microsoft.com/ru-ru/dotnet/desktop/wpf/overview/'),('Enumerable.FirstOrDefault','https://learn.microsoft.com/ru-ru/dotnet/api/system.linq.enumerable.firstordefault?view=net-10.0'),('Сериализация и десериализация JSON в .NET','https://learn.microsoft.com/ru-ru/dotnet/standard/serialization/system-text-json/overview')]
def linkify(line):
    return re.sub(r'https?://[^\s]+',lambda m:'<a href="'+m.group(0)+'">'+m.group(0)+'</a>',esc(line))
items=C.get('literature') or [t+'. Microsoft Learn. '+u+' (дата обращения: 03.11.2026).' for t,u in biblio]
literature='<article class="lesson">'+head('ПМ.04 · Для углубления','Литература и документация')+'<ol>'+''.join('<li>'+linkify(t)+'</li>' for t in items)+'</ol><p>Все обязательные действия раскрыты в комплекте; источники служат для углубления.</p></article>'

hero=head('Учебная практика · 4 курс · 7 семестр','Сопровождение программного обеспечения','Вход, возврат и совместный отбор в готовом приложении обслуживания оборудования.')
put('index.html',page('Задание',hero+assignment+material+buttons([('lessons.html','Перейти к разбору'),('downloads/assignment.pdf','Методические рекомендации · PDF'),('downloads/guide.pdf','Предметные области · PDF')]),'index.html'))
toc='<section class="contents"><h2>Порядок работы</h2><ol>'+''.join(f'<li><a href="#step-{i}">{esc(s["title"])}</a></li>' for i,s in enumerate(C['guide'],1))+'</ol></section>'
put('lessons.html',page('Методические рекомендации',head('ПМ.04 · Демонстрационный образец «Контур»','Методические рекомендации','Начать с общего задания, затем подготовить файлы и выполнить разбор по порядку.')+toc+method+literature+buttons([('areas.html','Выполнить свой вариант'),('downloads/assignment.pdf','Скачать методические рекомендации · PDF')]),'lessons.html'))

def area(a):
    ident='<div class="identity"><p><strong>Индивидуальная работа</strong>Данные и доказательства назначенной области.</p><p><strong>Образец — «Контур»</strong>Показывает способ диагностики и проверки.</p></div>'
    data=json.loads((ROOT/f'stands/data/variant-{a["id"]:02}.json').read_text(encoding='utf-8'))
    table='<section class="data"><h2>Исходные данные области</h2><table><thead><tr><th>Id и позиция</th><th>Категория и стоимость</th><th>Исполнение → остаток</th></tr></thead><tbody>'+''.join('<tr><td><strong>'+esc(p['Id'])+'</strong> · '+esc(p['Name'])+'</td><td>'+esc(p['Category'])+'<br>'+esc(p['Price'])+' руб.</td><td>'+'<br>'.join(esc(k)+' → '+str(v) for k,v in p['Stock'].items())+'</td></tr>' for p in data['Products'])+'</tbody></table></section>'
    stages=[a['blocks'][1],*a['blocks'][3:]]
    stages=[dict(b,title=re.sub(r'^\d+\.\s*','',b['title'])) for b in stages]
    return '<article class="area" id="variant-'+str(a['id'])+'" data-variant="'+str(a['id'])+'">'+head('ПМ.04 · Вариант '+f'{a["id"]:02}',a['title'].split('. ',1)[1],'Сопроводить готовое приложение; исправить три диагностических компонента.')+ident+table+'<ol class="route">'+''.join(card(b,i) for i,b in enumerate(stages,1))+'</ol>'+buttons([(f'downloads/variant-{a["id"]:02}.json','Данные этого варианта'),('downloads/Shop-source.zip','Исходный проект'),('lessons.html#step-1','Подготовка рабочей копии')])+'</article>'
areas=''.join(area(a) for a in C['areas'])
chooser='<div class="chooser"><label for="variant">Предметная область</label><select id="variant">'+''.join(f'<option value="{a["id"]}">{esc(a["title"])}</option>' for a in C['areas'])+'</select><button id="prev" type="button">Предыдущая</button><button id="next" type="button">Следующая</button><p id="position" aria-live="polite"></p></div>'
script='''<script>
const chooser=document.querySelector('#variant'), areas=[...document.querySelectorAll('.area')];
function show(n){n=Math.max(1,Math.min(areas.length,Number(n)||1));chooser.value=n;
 areas.forEach(a=>a.hidden=Number(a.dataset.variant)!==n);
 document.querySelector('#prev').disabled=n===1;document.querySelector('#next').disabled=n===areas.length;
 document.querySelector('#position').textContent=`Вариант ${n} из ${areas.length}`;
 history.replaceState(null,'','#variant-'+n);}
chooser.addEventListener('change',()=>show(chooser.value));
document.querySelector('#prev').addEventListener('click',()=>show(Number(chooser.value)-1));
document.querySelector('#next').addEventListener('click',()=>show(Number(chooser.value)+1));
show(location.hash.split('-').pop());
</script>'''
put('areas.html',page('Предметные области',chooser+areas+buttons([('downloads/guide.pdf','Все предметные области · PDF')]),'areas.html',script))
put('print-method.html',page('Методические рекомендации',hero+assignment+material+method+literature))
put('print-areas.html',page('Список предметных областей',head('Учебная практика ПМ.04','Список предметных областей','Диагностика и сопровождение приложений обслуживания оборудования.')+areas))

put('assets/style.css','''@font-face{font-family:Golos;src:url(fonts/GolosText-variable.ttf);font-weight:100 900;font-display:swap}
:root{--red:#ed131c;--ink:#1c1c1c;--soft:#f4f4f6;--line:#dfe1e5;--muted:#606773}
*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:90px}body{margin:0;background:#fff;color:var(--ink);font:15px/1.65 Golos,Arial,sans-serif}main{max-width:1000px;margin:0 auto;padding:28px 30px 64px}p{margin:0 0 12px}p:last-child{margin-bottom:0}a{color:#a30d15;text-underline-offset:3px}a:hover{text-decoration-thickness:2px}a:focus-visible,button:focus-visible,select:focus-visible{outline:3px solid var(--red);outline-offset:4px}nav{position:sticky;top:0;z-index:3;background:#fff;border-bottom:1px solid var(--line);display:flex;justify-content:center;gap:8px;padding:12px 20px;flex-wrap:wrap}nav a{padding:7px 14px;text-decoration:none;color:var(--ink);border-radius:6px}nav a[aria-current]{background:var(--ink);color:white}.skip{position:absolute;left:12px;top:-100px;z-index:5;background:white;padding:12px}.skip:focus{top:8px}
.chapter{background:var(--ink);color:white;border-bottom:6px solid var(--red);padding:32px 38px;margin:0 0 26px;break-inside:avoid}.chapter h1{font-size:35px;line-height:1.18;letter-spacing:-.8px;margin:20px 0 14px;font-weight:700}.chapter p{max-width:70ch}.kicker{font-size:12px;font-weight:600;display:flex;align-items:center;gap:10px}.kicker:before{content:'';width:25px;height:5px;background:var(--red);border-radius:2px;flex:none}.lesson,.area{margin-bottom:42px}.route{padding:0;margin:0;list-style:none}.step{display:grid;grid-template-columns:32px minmax(0,1fr);gap:12px;position:relative;padding-bottom:18px}.step:not(:last-child):after{content:'';position:absolute;left:15px;top:52px;bottom:0;width:1px;background:var(--line)}.number{display:grid;place-items:center;border:2px solid var(--red);border-radius:50%;width:32px;height:32px;margin-top:20px;background:white;font-size:13px}.card{background:var(--soft);border-radius:14px;padding:20px 24px;min-width:0}.card h3{font-size:18px;line-height:1.35;margin:0 0 14px;font-weight:600}.result{border-top:1px solid var(--line);padding-top:12px;margin-top:15px;font-size:14px}.illustration{list-style:none;margin:10px 0 28px 44px;break-inside:avoid}figure{margin:0}figure img{display:block;width:100%;height:auto;border:1px solid var(--line)}figcaption{font-size:13px;line-height:1.5;text-align:center;margin-top:10px}.identity{display:grid;grid-template-columns:1fr 1fr;gap:24px;padding:0 0 24px}.identity p+p{border-left:1px solid var(--line);padding-left:24px}.identity strong{display:block}.actions{display:flex;flex-wrap:wrap;gap:10px;margin:20px 0 35px}.button,button{display:inline-block;border:1px solid var(--line);border-radius:7px;background:white;color:var(--ink);padding:10px 16px;font:inherit;text-decoration:none;cursor:pointer;min-height:44px}.button:hover,button:hover{background:var(--soft)}button:disabled{opacity:.45;cursor:default}.contents,.chooser{padding:24px;border:1px solid var(--line);margin-bottom:30px;border-radius:14px}.contents h2{margin:0;font-size:20px}.chooser{display:flex;gap:12px;flex-wrap:wrap;align-items:center}.chooser label{font-weight:600}.chooser select{max-width:100%;flex:1 1 400px;min-height:44px;padding:10px;font:inherit;border:1px solid #767676;background:white}.chooser p{width:100%;font-size:13px;color:var(--muted);margin:0}pre{white-space:pre-wrap;overflow-wrap:anywhere;border:1px solid var(--line);border-radius:8px;background:white;padding:16px;margin:0;font:12px/1.6 Consolas,'Courier New',monospace}code{font-family:Consolas,'Courier New',monospace}h2,h3{break-after:avoid}[hidden]{display:none!important}
@media(max-width:650px){main{padding:16px 14px 40px}nav{position:static;padding:8px;gap:2px}nav a{font-size:13px;padding:8px}.chapter{padding:24px 22px}.chapter h1{font-size:29px;letter-spacing:-.5px}.card{padding:18px 16px}.step{grid-template-columns:27px minmax(0,1fr);gap:8px}.number{width:27px;height:27px}.step:not(:last-child):after{left:13px}.identity{grid-template-columns:1fr;gap:12px}.identity p+p{border-left:0;border-top:1px solid var(--line);padding:12px 0 0}.chooser{padding:18px}}
.data{margin:0 0 24px}.data h2{font-size:18px}table{border-collapse:collapse;width:100%;font-size:13px;line-height:1.5}td,th{text-align:left;border-bottom:1px solid var(--line);padding:9px;vertical-align:top}thead{display:table-header-group}th{background:var(--soft)}@page{size:A4 portrait;margin:20mm 20mm 20mm 30mm}
@media print{html{scroll-behavior:auto}body{font-family:Golos,Arial,sans-serif;font-size:12pt;line-height:1.6;color:black}main{max-width:none;padding:0;margin:0}nav,.skip,.chooser,.contents{display:none!important}.chapter{padding:7mm 8mm;margin-bottom:6mm;print-color-adjust:exact;-webkit-print-color-adjust:exact}.chapter h1{font-family:Golos,Arial,sans-serif;font-size:23pt;letter-spacing:-.4pt}.chapter p{font-size:12pt}.kicker{font-size:10pt!important}.lesson{margin-bottom:10mm}.area{break-before:page}.chapter{break-after:avoid}.lesson>ol:not(.route)>li{break-inside:avoid}.route{margin:0}.step{display:block;position:relative;padding:0 0 5mm 10mm;break-inside:avoid}.step:after{left:4mm!important;top:12mm!important}.number{position:absolute;left:0;top:0;width:7mm;height:7mm;margin-top:5mm;font:10pt Golos,Arial,sans-serif}.card{padding:5mm;background:var(--soft);border-radius:3mm;print-color-adjust:exact;-webkit-print-color-adjust:exact}.card h3{font-family:Golos,Arial,sans-serif;font-size:14pt}.card p{text-align:left;text-indent:0;orphans:3;widows:3}.result{font-size:12pt;text-indent:0!important}.identity{font-size:12pt;gap:6mm}.identity p{text-indent:0}.actions{display:none}pre{font:9pt/1.5 Consolas,'Courier New',monospace;padding:3mm}.area{margin-bottom:0}a{color:black}ol{padding-left:7mm}.illustration{margin:4mm 0 7mm 0;break-inside:avoid}figcaption{font-family:Times New Roman,serif;font-size:12pt;line-height:1.3}figure img{width:100%;max-height:175mm;object-fit:contain}table{font-size:10pt}th,td{padding:2mm}.data{break-inside:avoid}.data h2{font-size:14pt}}
''')

dl=SITE/'downloads';dl.mkdir(exist_ok=True)
for d in C['downloads']:
    src=ROOT/d['path'];dest=dl/d['name']
    if d['kind']=='project':
        with zipfile.ZipFile(dest,'w',zipfile.ZIP_DEFLATED) as z:
            for p in sorted(src.iterdir()):
                if p.is_file():z.write(p,p.relative_to(ROOT).as_posix())
    else:shutil.copyfile(src,dest)
for src,dst in [('stands/Shop-fixed/RepairLogic.cs','RepairLogic.cs'),('content/contract.md','contract.md')]:shutil.copyfile(ROOT/src,dl/dst)
report='# Отчёт по учебной практике ПМ.04\n\nФИО: ____________________\n\nГруппа: ____________________\n\nВариант и область: ____________________\n\nСсылка на репозиторий проекта: ____________________\n\n'
for i,s in enumerate(C['report']['sections'],1):report+=f'## {i}. {s["title"]}\n\n{s["prompt"]}\n\nОжидаемое до запуска: ____________________\n\nФактическое и доказательство: ____________________\n\n'
report+='## 6. Инструкция пользователя\n\nЗапуск с рабочим файлом, вход, совместный отбор, возврат, исправление ввода, отмена и подтверждение.\n\n## 7. Вывод\n\nСвязь причин и исправлений. Проверенные ограничения. Непроверенные условия.\n'
put('downloads/report-template.md',report)
manifest={'schema_version':1,'files':{p.relative_to(SITE).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(SITE.rglob('*')) if p.is_file() and p.name!='manifest.json'}}
put('manifest.json',json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
print('Built 3 student pages, 2 print sources, 30 datasets, 2 projects and report template.')
