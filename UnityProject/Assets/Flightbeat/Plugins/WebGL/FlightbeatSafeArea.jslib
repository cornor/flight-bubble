// 2026-11-06 (user: adapt to notches / the capsule, Douyin first). The real safe area and the mini-game capsule, for
// WechatBridge.SafeArea / Menu: "width,height,safeLeft,safeTop,safeRight,safeBottom,menuLeft,menuTop,menuRight,menuBottom"
// in the window's logical pixels (menu all 0 when there is none), or "" when nothing is known.
// Douyin: tt.getSystemInfoSync().safeArea + tt.getMenuButtonLayout(); WeChat: wx.getSystemInfoSync().safeArea +
// wx.getMenuButtonBoundingClientRect(). A browser uses CSS env(safe-area-inset-*); for checks, ?safe=top,bottom and
// ?menu=left,top,width,height (CSS px) simulate a phone.
mergeInto(LibraryManager.library, {
  FlightbeatSafeInfo: function () {
    var out = "";
    try {
      var api = (typeof tt !== "undefined" && tt && tt.getSystemInfoSync) ? tt
        : (typeof wx !== "undefined" && wx && wx.getSystemInfoSync) ? wx : null;
      if (api) {
        var info = api.getSystemInfoSync();
        var w = info.windowWidth || info.screenWidth, h = info.windowHeight || info.screenHeight;
        var a = info.safeArea || { left: 0, top: info.statusBarHeight || 0, right: w, bottom: h };
        var m = null;
        try {
          m = api.getMenuButtonLayout ? api.getMenuButtonLayout()
            : api.getMenuButtonBoundingClientRect ? api.getMenuButtonBoundingClientRect() : null;
        } catch (e) { m = null; }
        if (!m || !(m.width > 0) || !(m.height > 0)) m = { left: 0, top: 0, right: 0, bottom: 0 };
        out = [w, h, a.left, a.top, a.right, a.bottom, m.left, m.top, m.right, m.bottom].join(",");
      } else if (typeof window !== "undefined" && typeof document !== "undefined" && document.body) {
        var q = new URLSearchParams(window.location.search || "");
        var bw = window.innerWidth, bh = window.innerHeight, top = 0, bottom = 0, menu = [0, 0, 0, 0];
        if (q.get("safe")) {
          var p = q.get("safe").split(",");
          top = +p[0] || 0; bottom = +p[1] || 0;
        } else {
          var probe = document.createElement("div");
          probe.style.cssText = "position:fixed;visibility:hidden;pointer-events:none;" +
            "padding-top:env(safe-area-inset-top);padding-bottom:env(safe-area-inset-bottom)";
          document.body.appendChild(probe);
          var cs = getComputedStyle(probe);
          top = parseFloat(cs.paddingTop) || 0; bottom = parseFloat(cs.paddingBottom) || 0;
          document.body.removeChild(probe);
        }
        if (q.get("menu")) {
          var r = q.get("menu").split(",").map(Number);
          if (r.length === 4 && r[2] > 0 && r[3] > 0) menu = [r[0], r[1], r[0] + r[2], r[1] + r[3]];
        }
        out = [bw, bh, 0, top, bw, bh - bottom].concat(menu).join(",");
      }
    } catch (e) { out = ""; }
    var size = lengthBytesUTF8(out) + 1, buffer = _malloc(size);
    stringToUTF8(out, buffer, size);
    return buffer;
  }
});
