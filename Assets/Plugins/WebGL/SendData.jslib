mergeInto(LibraryManager.library, {
  SendDataToJS: function(ptr) {
    try {
      var json = UTF8ToString(ptr);
      // send structured message so parent knows it's Unity event data
      try { window.parent.postMessage({ type: 'UNITY_DATA', data: JSON.parse(json) }, '*'); }
      catch (e) { window.parent.postMessage({ type: 'UNITY_DATA', data: json }, '*'); }
    } catch (e) {
      try { window.parent.postMessage({ type: 'UNITY_DATA', data: String(ptr) }, '*'); } catch(_) {}
    }
  }
});
