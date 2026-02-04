mergeInto(LibraryManager.library, {
       GetSessionStorageItem: function (keyPtr) {
           var key = UTF8ToString(keyPtr);
           var value = window.sessionStorage.getItem(key) || "";
           var bufferSize = lengthBytesUTF8(value) + 1;
           var buffer = _malloc(bufferSize);
           stringToUTF8(value, buffer, bufferSize);
           return buffer;
       }
   });