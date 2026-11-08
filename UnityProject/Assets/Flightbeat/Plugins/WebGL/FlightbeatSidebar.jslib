// 2026-11-08 (user: Douyin's 侧边栏复访, a required review item since 2023-11-24). PixelWingGame.Sidebar reads:
//   FlightbeatSidebarSupport: 0 still asking, 1 not supported (no entry: other hosts, old Douyin), 2 supported
//     (tt.checkScene({ scene: 'sidebar' }).isExist).
//   FlightbeatSidebarFrom: 1 when the latest entry into the game came from the Douyin home sidebar - tt.onShow /
//     tt.getLaunchOptionsSync scene '021036', or launch_from 'homepage' + location 'sidebar_card'. The export patch
//     (platforms/douyin/sidebar_launch.py) starts that listener in game.js before the engine loads, as the platform
//     asks; without it the listener starts here on the first call.
//   FlightbeatSidebarOpen: tt.navigateToScene({ scene: 'sidebar' }); 1 when it was called.
// A browser simulates it with ?sidebar=1 (came from the sidebar) or ?sidebar=0 (supported, did not).
mergeInto(LibraryManager.library, {
  $FlightbeatSidebarState: function () {
    var g = typeof GameGlobal !== "undefined" ? GameGlobal : typeof window !== "undefined" ? window : {};
    var s = g.flightbeatSidebar || (g.flightbeatSidebar = { fromSidebar: false });
    if (!s.listening && typeof tt !== "undefined" && tt) {
      s.listening = true;
      var mark = function (o) {
        o = o || {};
        s.fromSidebar = o.scene === "021036" || (o.launch_from === "homepage" && o.location === "sidebar_card");
      };
      try { if (tt.getLaunchOptionsSync) mark(tt.getLaunchOptionsSync()); } catch (e) {}
      try { if (tt.onShow) tt.onShow(mark); } catch (e) {}
    }
    return s;
  },
  FlightbeatSidebarSupport__deps: ["$FlightbeatSidebarState"],
  FlightbeatSidebarSupport: function () {
    var s = FlightbeatSidebarState();
    if (s.support === undefined) {
      s.support = 0;
      try {
        if (typeof tt !== "undefined" && tt && tt.checkScene) {
          tt.checkScene({
            scene: "sidebar",
            success: function (r) { s.support = r && r.isExist ? 2 : 1; },
            fail: function () { s.support = 1; }
          });
        } else if (typeof window !== "undefined" && window.location && typeof URLSearchParams !== "undefined") {
          var q = new URLSearchParams(window.location.search || "").get("sidebar");
          if (q === "1" || q === "0") { s.support = 2; s.fromSidebar = q === "1"; } else s.support = 1;
        } else s.support = 1;
      } catch (e) { s.support = 1; }
    }
    return s.support;
  },
  FlightbeatSidebarFrom__deps: ["$FlightbeatSidebarState"],
  FlightbeatSidebarFrom: function () {
    return FlightbeatSidebarState().fromSidebar ? 1 : 0;
  },
  FlightbeatSidebarOpen: function () {
    try {
      if (typeof tt !== "undefined" && tt && tt.navigateToScene) {
        tt.navigateToScene({ scene: "sidebar", success: function () {}, fail: function (e) { console.log("navigateToScene", e); } });
        return 1;
      }
    } catch (e) {}
    return 0;
  }
});
