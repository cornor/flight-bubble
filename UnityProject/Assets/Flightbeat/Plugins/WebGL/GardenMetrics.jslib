mergeInto(LibraryManager.library, {
  GardenMetrics: function(json) {
    var sample = JSON.parse(UTF8ToString(json));
    window.gardenR1 = sample;
    if (!window.gardenR1History) window.gardenR1History = [];
    window.gardenR1History.push(Object.assign({at: performance.now()}, sample));
    if (window.gardenR1History.length > 3600) window.gardenR1History.shift();
  }
});
