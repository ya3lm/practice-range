mergeInto(LibraryManager.library, {
  PR_OpenFilePicker: function (acceptPtr, maxBytes, goNamePtr) {
    var accept = UTF8ToString(acceptPtr);
    var goName = UTF8ToString(goNamePtr);
    var input = document.getElementById('pr-file-input');
    if (!input) {
      input = document.createElement('input');
      input.type = 'file';
      input.id = 'pr-file-input';
      input.style.display = 'none';
      document.body.appendChild(input);
    }
    input.accept = accept;
    input.value = '';
    input.onchange = function () {
      var file = input.files && input.files[0];
      if (!file) return;
      var send = function (obj) {
        var inst = (typeof unityInstance !== 'undefined' && unityInstance) || (window.unityInstance);
        if (inst && inst.SendMessage) inst.SendMessage(goName, 'OnFileLoaded', JSON.stringify(obj));
        else if (typeof SendMessage === 'function') SendMessage(goName, 'OnFileLoaded', JSON.stringify(obj));
      };
      if (file.size > maxBytes) { send({ name: file.name, error: 'File exceeds ' + Math.round(maxBytes / 1048576) + ' MB' }); return; }
      var reader = new FileReader();
      reader.onload = function (e) {
        var bytes = new Uint8Array(e.target.result);
        var bin = '';
        var chunk = 0x8000;
        for (var i = 0; i < bytes.length; i += chunk) bin += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
        send({ name: file.name, base64: btoa(bin) });
      };
      reader.onerror = function () { send({ name: file.name, error: 'Read failed' }); };
      reader.readAsArrayBuffer(file);
    };
    input.click();
  }
});
