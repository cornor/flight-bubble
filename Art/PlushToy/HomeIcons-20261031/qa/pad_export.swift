import AppKit
import Foundation

// Technical PNG export: preserve the generated icon inside a 5 px clear border.
let args = CommandLine.arguments
let input = NSBitmapImageRep(data: try Data(contentsOf: URL(fileURLWithPath: args[1])))!
precondition(input.pixelsWide == 150 && input.pixelsHigh == 150 && input.samplesPerPixel == 4)
let output = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: 160, pixelsHigh: 160,
    bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
    colorSpaceName: .deviceRGB, bitmapFormat: .alphaNonpremultiplied,
    bytesPerRow: 0, bitsPerPixel: 0)!
for y in 0..<160 { for x in 0..<160 {
    var pixel = [Int](repeating: 0, count: 4)
    if x >= 5 && x < 155 && y >= 5 && y < 155 {
        input.getPixel(&pixel, atX: x-5, y: y-5)
    }
    output.setPixel(&pixel, atX: x, y: y)
} }
try output.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: args[2]))
print("Saved 160x160 RGBA with 5 px clear border:", args[2])
