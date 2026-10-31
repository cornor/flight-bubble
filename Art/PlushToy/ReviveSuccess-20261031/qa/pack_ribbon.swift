import AppKit
import Foundation

// Export-only periodic edge packing of ImageGen artwork, in premultiplied RGBA.
// This preserves all pixels outside the small boundary overlap.
let args = CommandLine.arguments
let rep = NSBitmapImageRep(data: try Data(contentsOf: URL(fileURLWithPath: args[1])))!
let width = rep.pixelsWide, height = rep.pixelsHigh
precondition(width == 1080 && height == 200 && rep.samplesPerPixel == 4)
let overlap = 48
for y in 0..<height {
    for x in 0..<overlap {
        var left = [Int](repeating: 0, count: 4), right = left
        rep.getPixel(&left, atX: x, y: y)
        rep.getPixel(&right, atX: width-1-x, y: y)
        let weight = 0.5 * pow(cos(Double(x) / Double(overlap-1) * .pi / 2), 2)
        func mix(_ a: [Int], _ b: [Int]) -> [Int] {
            let alpha = Double(a[3])*(1-weight) + Double(b[3])*weight
            var out = [Int](repeating: 0, count: 4)
            out[3] = Int(alpha.rounded())
            if alpha > 0 {
                for c in 0..<3 {
                    out[c] = Int(((Double(a[c]*a[3])*(1-weight) + Double(b[c]*b[3])*weight) / alpha).rounded())
                }
            }
            return out
        }
        var a = mix(left, right), b = mix(right, left)
        // Explicit identical endpoint pixels, including invisible RGB.
        if x == 0 { b = a }
        rep.setPixel(&a, atX: x, y: y)
        rep.setPixel(&b, atX: width-1-x, y: y)
    }
}
try rep.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: args[2]))
print("Periodic boundary packed:", overlap, "pixels per side; interior unchanged.")
