import AppKit
import Foundation
let a=CommandLine.arguments
func read(_ path:String)->NSBitmapImageRep {NSBitmapImageRep(data:try! Data(contentsOf:URL(fileURLWithPath:path)))!}
func blank(_ w:Int,_ h:Int)->NSBitmapImageRep {NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:w,pixelsHigh:h,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bitmapFormat:.alphaNonpremultiplied,bytesPerRow:0,bitsPerPixel:0)!}
func sample(_ im:NSBitmapImageRep,_ x:Double,_ y:Double)->[Double] {
 let xx=Int(floor(x)),yy=Int(floor(y)),fx=x-floor(x),fy=y-floor(y)
 var p=[Double](repeating:0,count:4)
 for j in 0...1 {for i in 0...1 {
  let sx=xx+i,sy=yy+j;if sx<0 || sy<0 || sx>=im.pixelsWide || sy>=im.pixelsHigh {continue}
  var q=[Int](repeating:0,count:4);im.getPixel(&q,atX:sx,y:sy)
  let w=(i==0 ? 1-fx:fx)*(j==0 ? 1-fy:fy),al=im.hasAlpha ? Double(q[3])/255:1.0
  p[3]+=al*w;for c in 0..<3 {p[c]+=Double(q[c])*al*w}
 }}
 return p
}
func save(_ im:NSBitmapImageRep,_ path:String) {try! im.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:path))}
let cfg=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:a[2]))) as! [String:Any]
if a[1]=="pack" {
 let entries=cfg["assets"] as! [[String:Any]]
 for e in entries {
  let im=read(e["source"] as! String),sz=e["size"] as! [Int],box=e["crop"] as! [Double],out=blank(sz[0],sz[1]),capsule=e["capsule"] as! Bool
  let sw=box[2]-box[0],sh=box[3]-box[1],keepAspect=e["keepAspect"] as? Bool ?? false
  let fit=min(Double(sz[0]-4)/sw,Double(sz[1]-4)/sh)
  let w=keepAspect ? sw*fit:Double(sz[0]-4),h=keepAspect ? sh*fit:Double(sz[1]-4)
  let ox0=(Double(sz[0])-w)/2,oy0=(Double(sz[1])-h)/2
  for y in 0..<sz[1] {for x in 0..<sz[0] {
   var p=[Int](repeating:0,count:4)
   if Double(x)>=ox0 && Double(y)>=oy0 && Double(x)<ox0+w && Double(y)<oy0+h {
    var accum=[Double](repeating:0,count:4)
    for oy in [-0.25,0.25] {for ox in [-0.25,0.25] {
     let u=Double(x)-ox0+0.5+ox,v=Double(y)-oy0+0.5+oy
     var sx=u/w*sw
     if capsule {
      let cap=h/2,sourceCap=sh/2
      if u<cap {sx=u/h*sh}
      else if u>w-cap {sx=sw-(w-u)/h*sh}
      else {sx=sourceCap+(u-cap)/(w-2*cap)*(sw-2*sourceCap)}
     }
     let q=sample(im,box[0]+sx-0.5,box[1]+v/h*sh-0.5)
     for c in 0..<4 {accum[c]+=q[c]/4}
    }}
    if accum[3]*255>8 {p=[Int((accum[0]/accum[3]).rounded()),Int((accum[1]/accum[3]).rounded()),Int((accum[2]/accum[3]).rounded()),min(255,Int((accum[3]*255).rounded()))]}
   }
   out.setPixel(&p,atX:x,y:y)
  }}
  save(out,e["delivery"] as! String)
 }
} else {
 let sz=cfg["size"] as! [Int],out=blank(sz[0],sz[1]),bg=cfg["background"] as! [Int]
 let layers=(cfg["layers"] as! [[String:Any]]).map {e -> (NSBitmapImageRep,[Double],[Double],Double,Double,[Double]) in
  let im=read(e["source"] as! String),pos=e["center"] as! [Double],display=e["display"] as! [Double],t=(e["rotation"] as? Double ?? 0)*Double.pi/180
  let crop=e["crop"] as? [Double] ?? [0,0,Double(im.pixelsWide),Double(im.pixelsHigh)]
  return(im,pos,display,cos(t),sin(t),crop)
 }
 for y in 0..<sz[1] {for x in 0..<sz[0] {
  var d=bg
  for (im,pos,display,cs,sn,crop) in layers {
   let xx=Double(x)-pos[0],yy=Double(y)-pos[1],u=(xx*cs+yy*sn)/display[0]+0.5,v=(-xx*sn+yy*cs)/display[1]+0.5
   if u<0 || u>=1 || v<0 || v>=1 {continue}
   let p=sample(im,crop[0]+u*(crop[2]-crop[0])-0.5,crop[1]+v*(crop[3]-crop[1])-0.5),da=Double(d[3])/255,oa=p[3]+da*(1-p[3])
   for c in 0..<3 {d[c]=oa>0 ? Int(((p[c]+Double(d[c])*da*(1-p[3]))/oa).rounded()):0}
   d[3]=min(255,Int((oa*255).rounded()))
  }
  out.setPixel(&d,atX:x,y:y)
 }}
 save(out,cfg["output"] as! String)
}
