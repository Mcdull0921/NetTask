//@ sourceURL= log.js
var Log=new (function (){
    var ajax=function(method,datas,callback){
        var url=method;
        if(datas){
            var pa='';
            for(var k in datas){
                pa+=k+'='+datas[k]+'&'
            }
            url+='?'+pa.substr(0,pa.length-1);
        }
        $.get(basepath + url, function (res) {
            if (res.State == 0) {
                alert(res.Msg);
                return;
            }
            if(callback)
                callback(res.Data)
        });
    };

    var icons=['info','danger'];
    var levels=['info','error']

    this.getLogFiles=function() {
        ajax('GetLogFiles',{'number':20},function (data) {
            var html='';
            for(var i=0;i<data.length;i++){
                var d=JSON.parse(data[i]);
                html += "<h6>"+ d.name +"</h6>";
                for (var j =0;j<d.logs.length;j++) {
                    html += "<button type='button' onclick='getLogFile(\""+ (d.name+'$'+levels[d.logs[j].level]+'$'+d.logs[j].name) +"\")' class='btn btn-outline-"+ icons[d.logs[j].level] +" mb-2'><span data-feather='file'></span> "
                        + d.logs[j].name+ "</button> ";
                }
            }
            $("#divOldLog").html(html);
        });
    };

})();
(function () {
    $(document).ready(function () {
        Log.getLogFiles();
    });
})();





// (function () {
//     refreshLog();
// }());

// function getLogFiles() {
//     $.get(basepath
//         + "GetLogFiles",
//         function (res) {
//             var data = res.Data;
//             var html = "<label>往期日志：</label><br />";
//             for (i in data) {
//                 html += "<button type='button' onclick='getLogFile(\""
//                     + data[i] + "\")' class='btn btn-outline-warning mb-2'><span data-feather='file'></span>"
//                     + data[i]
//                     + "</button> ";
//                 i++;
//             }
//             $("#divOldLog").html(html);
//         }
//     );
// };

// function getLogFile(filekey) {
//     var apiUrl = basepath + "GetLogFile?filekey=" + filekey;
//     window.open(apiUrl);
// }

// function getLogFileInfo(lines) {
//     var apiUrl = basepath + "GetLogFileInfo?lastLines=" + lines;
//     $.get(apiUrl,
//         function (res) {
//             var data = res.Data;
//             var logText = "";
//             for (i in data) {
//                 logText += data[i] + "\r\n";
//             }
//             $("#tbxLog").val(logText);
//         }
//     );
// }

// function refreshLog() {
//     getLogFiles();
//     getLogFileInfo(20);
//     setTimeout(scrollToBottom, 200);
// }

// function scrollToBottom() {
//     $('#tbxLog').scrollTop($('#tbxLog')[0].scrollHeight);
// }

