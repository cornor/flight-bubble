(() => {
  if (!new URLSearchParams(location.search).has('perf')) return;
  const panel = document.createElement('aside');
  panel.style.cssText = 'position:fixed;z-index:10;right:6px;top:6px;padding:8px;background:#fffdeeeF;color:#302c25;font:12px system-ui;border:1px solid #776750;border-radius:6px;max-width:210px';
  panel.innerHTML = '<div id="r1-metrics">等待游戏启动…</div><button id="r1-export">导出测量 JSON</button>';
  document.body.appendChild(panel);
  setInterval(() => {
    const m=window.gardenR1;
    if(m)document.getElementById('r1-metrics').textContent=`第 ${m.level+1} 关 | 屏内 ${m.visibleBugs} 虫\n${m.fps.toFixed(1)} FPS | P95 ${m.p95ms.toFixed(1)} ms`;
  }, 1000);
  document.getElementById('r1-export').onclick=()=>{
    const canvas=document.querySelector('canvas'),gl=canvas.getContext('webgl2')||canvas.getContext('webgl');
    const ext=gl?.getExtension('WEBGL_debug_renderer_info');
    const report={userAgent:navigator.userAgent,hardwareConcurrency:navigator.hardwareConcurrency,deviceMemory:navigator.deviceMemory,viewport:[innerWidth,innerHeight],backing:[canvas.width,canvas.height],dpr:devicePixelRatio,renderer:ext?gl.getParameter(ext.UNMASKED_RENDERER_WEBGL):null,samples:window.gardenR1History||[],note:'真实 Unity Playing 帧。排除暂停、菜单和 Intro；本数据不识别设备是否低端安卓。'};
    const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([JSON.stringify(report,null,2)],{type:'application/json'}));a.download='garden-r1-performance.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000);
  };
})();
