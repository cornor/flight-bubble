import AppKit
import Foundation

// Technical animation-frame packing. Both art sources are ImageGen outputs.
// Copy only the generated eyelid regions; retain the idle frame elsewhere.
let root = URL(fileURLWithPath: CommandLine.arguments[1])
let idle = NSBitmapImageRep(data: try Data(contentsOf: root.appendingPathComponent("delivery/home/bear-idle.png")))!
let eyes = NSBitmapImageRep(data: try Data(contentsOf: root.appendingPathComponent("qa/blink-generated-resized.png")))!
precondition(idle.pixelsWide == 1080 && idle.pixelsHigh == 1080)
precondition(eyes.pixelsWide == 1080 && eyes.pixelsHigh == 1080)
precondition(idle.samplesPerPixel == 4 && eyes.samplesPerPixel == 4)
let sourcePolygons: [[[Double]]] = [
    [[471,448],[471,368],[488,324],[519,298],[558,296],[592,307],[620,331],[636,344],[623,366],[609,390],[594,411],[579,433],[575,454],[533,460],[494,457]],
    [[660,227],[694,213],[735,218],[772,240],[791,267],[795,297],[783,311],[758,310],[742,304],[728,294],[708,287],[691,290],[675,300],[660,312],[654,282]]
]
let polygons = sourcePolygons.map { $0.map { CGPoint(x: $0[0] * 1080 / 1254, y: $0[1] * 1080 / 1254) } }
func inside(_ p: CGPoint, _ polygon: [CGPoint]) -> Bool {
    var result = false
    var j = polygon.count - 1
    for i in polygon.indices {
        let a = polygon[i], b = polygon[j]
        if (a.y > p.y) != (b.y > p.y) && p.x < (b.x-a.x) * (p.y-a.y) / (b.y-a.y) + a.x { result.toggle() }
        j = i
    }
    return result
}
func distance(_ p: CGPoint, _ a: CGPoint, _ b: CGPoint) -> Double {
    let dx = b.x-a.x, dy = b.y-a.y
    let t = max(0, min(1, ((p.x-a.x)*dx + (p.y-a.y)*dy) / (dx*dx+dy*dy)))
    return hypot(p.x-a.x-t*dx, p.y-a.y-t*dy)
}
var changed = 0
for y in 0..<1080 { for x in 0..<1080 {
    let p = CGPoint(x: Double(x)+0.5, y: Double(y)+0.5)
    var weight = 0.0
    for polygon in polygons where inside(p, polygon) {
        let edge = polygon.indices.map { distance(p, polygon[$0], polygon[($0+1) % polygon.count]) }.min()!
        weight = max(weight, min(1, edge / 3.0))
    }
    if weight <= 0 { continue }
    var a = [Int](repeating: 0, count: 4), b = a
    idle.getPixel(&a, atX: x, y: y); eyes.getPixel(&b, atX: x, y: y)
    for c in 0..<3 { a[c] = Int((Double(a[c])*(1-weight) + Double(b[c])*weight).rounded()) }
    idle.setPixel(&a, atX: x, y: y)
    changed += 1
} }
try idle.representation(using: .png, properties: [:])!.write(to: root.appendingPathComponent("delivery/home/bear-blink.png"))
let info: [String: Any] = ["method":"ImageGen eyelid pixels packed into idle frame; no change outside eye polygons; preserve original alpha", "source_canvas":[1254,1254],"output_canvas":[1080,1080],"source_eye_polygons":sourcePolygons,"feather_pixels":3,"packed_pixels":changed]
try JSONSerialization.data(withJSONObject:info,options:[.prettyPrinted,.sortedKeys]).write(to:root.appendingPathComponent("qa/blink-packing.json"))
print("Packed generated eyelid pixels:", changed)
