var Task=new (function (){
    this.getTasks=function() {
        $.get(basepath + "GetTasks", function (res) {
            var data = res.Data;
            var htmlStr = "";
            for (var i=0; i< data.length;i++) {
                var task = $.parseJSON(data[i]);
                htmlStr += "<tr>" +
                    "<td><span title='"+ task.id +"'>" + task.id.substr(0,8)+'...' + "</span></td>" +
                    "<td>" + task.name + "</td>" +
                    "<td>" + task.typeName + "</td>" +
                    "<td>" + task.status + "</td>" +
                    "<td>" + task.nextProcessTime + "</td>" +
                    "<td>" + task.timerType + "</td>" +
                    "<td>" + task.interval + "</td>" +
                    "<td>" + task.runOnStart + "</td>" +
                    "<td>" + task.startTime + "</td>" ;
                htmlStr += "<td>" + dropDownButtonHtml(task.id) + "</td>" +
                    "</tr>";
            }
            $("#tb_body").html(htmlStr);
            if (feather)
                feather.replace();    
        });
    };

    this.startAll=function(){
        ajax(this,'StartAllTasks');
    };

    this.stopAll=function(){
        ajax(this,'StopAllTasks');
    };

    this.startTask=function(id){
        this.ajax(this,'StartTask',id);
    };

    this.stopTask=function(id){
        ajax(this,'StopTask',id);
    };

    this.runTask=function(id){
        ajax(this,'RunTask',id);
    };

    var ajax=function(self,method,id){
        var url=method;
        if(id)url+="?id="+id;
        $.get(basepath + url, function (res) {
            if (res.State == 0) {
                alert(res.Msg);
                return;
            }
            self.getTasks();
        });
    };

    this.editRunParam=function(){
         $("#editConfig").collapse('hide');
        if(!$("#editRunParam").is(':visible')){
            $("#editRunParam").collapse('show');
        }
        
    };

    this.editConfig=function(){
         $("#editRunParam").collapse('hide');
        if(!$("#editConfig").is(':visible')){
            $("#editConfig").collapse('show');
        }
    };


   var dropDownButtonHtml= function (id) {
        var html = "<div class=\"btn-group\" '>" +
            "<button class=\"btn btn-outline-secondary btn-sm dropdown-toggle\" type=\"button\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"false\">" +
            "操作</button>\r\n      <div class=\"dropdown-menu dropdown-menu-right\" x-placement=\"bottom-start\" style=\"position: absolute; will-change: transform; top: 0px; left: 0px; transform: translate3d(0px, 31px, 0px);\">";
            
        html += "<a class=\"dropdown-item\" href=\"javascript:Task.startTask('" + id+ "')\">开始任务</a>"+
        "<a class=\"dropdown-item\" href=\"javascript:Task.stopTask('" + id + "')\">停止任务</a>"+
        "<a class=\"dropdown-item\" href=\"javascript:Task.runTask('" + id + "')\">立即执行</a>"+
        "<div class=\"dropdown-divider\"></div>" +
            "<a class=\"dropdown-item\" href=\"javascript:Task.editRunParam('" + id + "',)\">修改运行参数</a>" +
            "<a class=\"dropdown-item\" href=\"javascript:Task.editConfig('" + id + "')\">修改任务配置</a>" +
            "</div></div>";
        return html;
    };
})();
(function () {
    $(document).ready(function () {
        $("#cbAutoRefresh").change(function(e) { 
           if($(e.target).prop('checked')){
                Task.autoRefresh = setInterval(Task.getTasks,1000);
           }
           else{
                window.clearInterval(Task.autoRefresh);
           }
        }); 
        Task.getTasks();
    });
})();




