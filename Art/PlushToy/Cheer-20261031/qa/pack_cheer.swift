import AppKit
import Foundation

// Register ImageGen's edited toy, then copy protected original base pixels verbatim.
let a=CommandLine.arguments
let idle=NSBitmapImageRep(data:try Data(contentsOf:URL(fileURLWithPath:a[1])))!
let gen=NSBitmapImageRep(data:try Data(contentsOf:URL(fileURLWithPath:a[2])))!
let cfg=try JSONSerialization.jsonObject(with:Data(contentsOf:URL(fileURLWithPath:a[3]))) as! [String:Any]
let curve=cfg["cutline"] as! [Double]
let scale=cfg["scale"] as! Double, dx=cfg["dx"] as! Double, dy=cfg["dy"] as! Double
let leafOffsets=cfg["leaf_alignment_offsets"] as! [Double]
let protected=NSBitmapImageRep(data:try Data(contentsOf:URL(fileURLWithPath:cfg["protection_mask_path"] as! String)))!
func isProtected(_ x:Int,_ y:Int)->Bool {
 var pixel=[Int](repeating:0,count:4);protected.getPixel(&pixel,atX:x,y:min(255,max(0,y)));return pixel[0]>127
}
let out=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:224,pixelsHigh:256,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bitmapFormat:.alphaNonpremultiplied,bytesPerRow:0,bitsPerPixel:0)!
func sample(_ x:Double,_ y:Double)->[Int]{
 let x0=Int(floor(x)),y0=Int(floor(y));let fx=x-Double(x0),fy=y-Double(y0)
 var sums=[Double](repeating:0,count:4)
 for j in 0...1 {for i in 0...1 {
  let xx=x0+i,yy=y0+j;if xx<0 || xx>=224 || yy<0 || yy>=256 {continue}
  var p=[Int](repeating:0,count:4);gen.getPixel(&p,atX:xx,y:yy)
  let w=(i==0 ? 1-fx:fx)*(j==0 ? 1-fy:fy);sums[3]+=Double(p[3])*w
  for c in 0..<3 {sums[c]+=Double(p[c]*p[3])*w}
 }}
 if sums[3]<0.5{return [0,0,0,0]}
 return [Int((sums[0]/sums[3]).rounded()),Int((sums[1]/sums[3]).rounded()),Int((sums[2]/sums[3]).rounded()),Int(sums[3].rounded())]
}
for y in 0..<256 {for x in 0..<224 {
 var p=[Int](repeating:0,count:4)
 if isProtected(x,y) {idle.getPixel(&p,atX:x,y:y)}
 else {
  // Register only columns with a regenerated leaf edge above the original.
  // A 30px ramp keeps the map monotone and the face center undistorted.
  let shift=leafOffsets[x]*max(0,min(1,(Double(y)-curve[x]+30)/30))
  p=sample((Double(x)-dx)/scale,(Double(y)-shift-dy)/scale)
  let w=max(0,min(1,(Double(y)-curve[x]+3)/3))
  if w>0 && Double(y)<curve[x] && isProtected(x,Int(ceil(curve[x]))) {var q=[Int](repeating:0,count:4);idle.getPixel(&q,atX:x,y:y)
   let alpha=Double(p[3])*(1-w)+Double(q[3])*w
   for c in 0..<3 {p[c]=alpha>0 ? Int(((Double(p[c]*p[3])*(1-w)+Double(q[c]*q[3])*w)/alpha).rounded()):0}
   p[3]=Int(alpha.rounded())
  }
  if x<3 || x>220 || y<3 || y>252 {p=[0,0,0,0]}
 }
 out.setPixel(&p,atX:x,y:y)
}}
try out.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:a[4]))
