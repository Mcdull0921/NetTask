var Assembly=new (function (){
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

    this.delAssembly=function(id){
        var self=this;
        if(confirm("删除程序集将再也无法恢复，确认删除吗？")){
            ajax("DelAssembly",{'id':id},function(){
                alert('删除成功！');
                self.getData();
            });
        }
    };

    this.fileSelected =function() {
        var file = document.getElementById('fileToUpload').files[0];
        if (file) {
            if(file.name.substr(file.name.lastIndexOf('.')).toLowerCase()!='.zip'){
                alert('只能上传zip后缀的文件，请将程序集打包成zip格式上传！');
                $("#fileToUpload").val("");
                return;
            }
            var fileSize = 0;
            if (file.size > 1024 * 1024)
                fileSize = (Math.round(file.size * 100 / (1024 * 1024)) / 100).toString() + 'MB';
            else
                fileSize = (Math.round(file.size * 100 / 1024) / 100).toString() + 'KB';
            $('#fileName').html('文件名: '+file.name);
            $('#fileSize').html('Size: ' + fileSize);
        }
        uploadFile(this);
    
    }

    var uploadFile=function (self) {
        var fd = new FormData();
        fd.append("fileToUpload", document.getElementById('fileToUpload').files[0]);
        var xhr = new XMLHttpRequest();
        xhr.upload.addEventListener("progress", function(evt){
            if (evt.lengthComputable) {
                var percentComplete = Math.round(evt.loaded * 100 / evt.total);
                $('#progressNumber').html(percentComplete.toString() + '%');
            }
            else {
                $('#progressNumber').html('unable to compute');
            }
        }, false);
        xhr.addEventListener("load", function(evt){
    /* 服务器端返回响应时候触发event事件*/
    var result = JSON.parse(evt.target.responseText);
            if (result.State == 1) {
                alert('上传成功！');
                self.getData();
            }
            else{
                alert(result.Msg);
            }
            $("#fileToUpload").val("");
            $('#fileName').html('');
            $("#fileSize").html("");
            $("#progressNumber").html("");
        }, false);
        xhr.addEventListener("error", function(){
            alert("上传失败!");
        }, false);
        xhr.addEventListener("abort", function(){
            alert("客户端中断了上传。");
        }, false);
        xhr.open("POST", basepath + "UploadAssembly");//修改成自己的接口 xhr.send(fd);
        xhr.send(fd);
    };

    this.getData=function() {
        ajax("GetAssemblies",null,function(data){
            var htmlStr = "";
            for (var i=0; i< data.length;i++) {
                var d = $.parseJSON(data[i]);
                var operate = ' <div class="btn-group" role="group">'
                +'<button type="button" class="btn btn-sm btn-outline-secondary" onclick="Assembly.delAssembly(\''+d.id+'\')"><span data-feather="x"></span> 删除</button>'
                +'</div>';
                htmlStr += "<tr>" +
                "<td>"+d.id+"</td>" +
                "<td>"+ new Date(d.create).format('YYYY-mm-dd HH:MM:SS')+"</td>" +
                "<td>"+operate+"</td></tr>" +
                "<tr><td colspan=\"3\">"+
                "<div style=\"margin-bottom: 10px;\" ><span class=\"title\">任务：</span>"+
                $.map(d.tasks,function(i){ return '<span class="label label-task">'+ i +'</span>\n'; }).join('')+ '  </div>'+
                '<div><span class="title">文件：</span>'+
                $.map(d.files,function(i){ return '<span class="label">'+ i +'</span>\n'; }).join('')+ '  </div>'+
                "</td></tr>";
            }
            $("#tb_body").html(htmlStr);
            if (feather)
                feather.replace(); 
        });
    };
})();
(function () {
    $(document).ready(function () {
        Assembly.getData();
    });
})();




