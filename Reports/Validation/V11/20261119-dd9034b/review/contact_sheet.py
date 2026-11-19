import sys
from PIL import Image, ImageDraw
out=sys.argv[1]; files=sys.argv[2:]
w,h=800,450; cols=3; rows=(len(files)+cols-1)//cols
sheet=Image.new("RGB",(w*cols,(h+24)*rows),(20,20,20)); d=ImageDraw.Draw(sheet)
for i,f in enumerate(files):
    im=Image.open(f).convert("RGB").resize((w,h)); x=(i%cols)*w; y=(i//cols)*(h+24)
    sheet.paste(im,(x,y+24)); d.text((x+6,y+6),f.split("/")[-2]+"/"+f.split("/")[-1],fill=(255,255,0))
sheet.save(out)
