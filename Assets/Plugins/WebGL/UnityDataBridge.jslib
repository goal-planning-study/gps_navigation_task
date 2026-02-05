mergeInto(LibraryManager.library, {
       SendDataToJS: function (jsonDataPtr) {
           var jsonData = UTF8ToString(jsonDataPtr);
           try {
               var data = JSON.parse(jsonData);
               if (window.parent && window.parent !== window) {
                   window.parent.postMessage({
                       type: 'UNITY_DATA',
                       data: data
                   }, '*');
               }
               console.log('[Unity -> JS]:', data);
           } catch (e) {
               console.error('Failed to parse Unity data:', e);
           }
       }
   });