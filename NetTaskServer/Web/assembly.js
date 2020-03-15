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
        if(confirm("删除程序集将再也无法恢复，确认删除吗？")){
            alert('删除成功！');
            ajax("DelAssembly",{'id':id},this.getData);
        }
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




