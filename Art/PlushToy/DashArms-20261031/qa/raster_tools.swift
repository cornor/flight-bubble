import AppKit
import Foundation

let args=CommandLine.arguments
func read(_ p:String)->NSBitmapImageRep { NSBitmapImageRep(data:try! Data(contentsOf:URL(fileURLWithPath:p)))! }
func blank()->NSBitmapImageRep {
 NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:512,pixelsHigh:512,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bitmapFormat:.alphaNonpremultiplied,bytesPerRow:0,bitsPerPixel:0)!
}
func sample(_ im:NSBitmapImageRep,_ x:Double,_ y:Double)->[Int] {
 let xx=Int(floor(x)),yy=Int(floor(y)),fx=x-floor(x),fy=y-floor(y)
 var s=[Double](repeating:0,count:4)
 for j in 0...1 {for i in 0...1 {
  let sx=xx+i,sy=yy+j
  if sx<0 || sy<0 || sx>=im.pixelsWide || sy>=im.pixelsHigh {continue}
  var p=[Int](repeating:0,count:4);im.getPixel(&p,atX:sx,y:sy)
  let w=(i==0 ? 1-fx:fx)*(j==0 ? 1-fy:fy)
  s[3]+=Double(p[3])*w
  for c in 0..<3 {s[c]+=Double(p[c]*p[3])*w}
 }}
 if s[3]<1.5 {return [0,0,0,0]}
 return [Int((s[0]/s[3]).rounded()),Int((s[1]/s[3]).rounded()),Int((s[2]/s[3]).rounded()),min(255,Int(s[3].rounded()))]
}
if args[1]=="transform" {
 let im=read(args[2]),out=blank()
 let ax=Double(args[4])!,ay=Double(args[5])!,dx=Double(args[6])!,dy=Double(args[7])!,scale=Double(args[8])!,theta=Double(args[9])! * .pi/180
 let cs=cos(theta),sn=sin(theta)
 for y in 0..<512 {for x in 0..<512 {
  let xx=(Double(x)-dx)/scale,yy=(Double(y)-dy)/scale
  var p=sample(im,ax+xx*cs+yy*sn,ay-xx*sn+yy*cs)
  out.setPixel(&p,atX:x,y:y)
 }}
 try out.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:args[3]))
} else if args[1]=="composite" {
 let out=blank(),ims=args.dropFirst(4).map{read($0)}
 let bg=args[3],n=UInt32(bg,radix:16) ?? 0
 let base=bg=="transparent" ? [0,0,0,0]:[Int((n>>16)&255),Int((n>>8)&255),Int(n&255),255]
 for y in 0..<512 {for x in 0..<512 {
  var d=base
  for im in ims {
   var p=[Int](repeating:0,count:4);im.getPixel(&p,atX:x,y:y)
   let a=Double(p[3])/255,da=Double(d[3])/255,oa=a+da*(1-a)
   for c in 0..<3 {d[c]=oa>0 ? Int(((Double(p[c])*a+Double(d[c])*da*(1-a))/oa).rounded()):0}
   d[3]=Int((oa*255).rounded())
  }
  out.setPixel(&d,atX:x,y:y)
 }}
 try out.representation(using:.png,properties:[:])!.write(to:URL(fileURLWithPath:args[2]))
}
