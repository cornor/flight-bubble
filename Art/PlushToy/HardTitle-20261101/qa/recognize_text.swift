import Foundation
import Vision
import AppKit
let image=NSImage(contentsOfFile:CommandLine.arguments[1])!
let cg=image.cgImage(forProposedRect:nil,context:nil,hints:nil)!
let request=VNRecognizeTextRequest()
request.recognitionLevel = .accurate
request.recognitionLanguages=["zh-Hans","en-US"]
request.usesLanguageCorrection=false
try VNImageRequestHandler(cgImage:cg).perform([request])
for r in request.results ?? [] {for c in r.topCandidates(3) {print(c.string+" | "+String(c.confidence))}}
