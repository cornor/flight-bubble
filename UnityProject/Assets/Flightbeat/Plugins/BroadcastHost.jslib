// Source: Reverse/input/framework.js, _PixelWingBroadcastStatus (host boundary).
mergeInto(LibraryManager.library, {
  PixelWingBroadcastStatus: function (ptr) {
    var status = JSON.parse(UTF8ToString(ptr));
    var node = document.getElementById("broadcast-status");
    if (node) {
      node.dataset.status = JSON.stringify(status);
      node.dataset.audioState = typeof WEBAudio !== "undefined" && WEBAudio.audioContext
        ? WEBAudio.audioContext.state : "unavailable";
    }
  }
});
