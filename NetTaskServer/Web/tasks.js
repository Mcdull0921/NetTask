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
        ajax('StartAllTasks',null,this.getTasks);
    };

    this.stopAll=function(){
        ajax('StopAllTasks',null,this.getTasks);
    };

    this.startTask=function(id){
        ajax('StartTask',{'id':id},this.getTasks);
    };

    this.stopTask=function(id){
        ajax('StopTask',{'id':id},this.getTasks);
    };

    this.runTask=function(id){
        ajax('RunTask',{'id':id},this.getTasks);
    };

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

    this.editRunParam=function(id){
         $("#editConfig").collapse('hide');
         ajax('GetTask',{'id':id},function(data){
            $('#taskId').val(data.Id);
            $('#taskName').val(data.Name);
            $('#taskTypeName').val(data.TypeName);
            $('#timeType').val(data.TaskTimerType);
            $('#interval').val(data.Interval);
            if(data.StartTime){
                $('#startTime').val(new Date(data.StartTime).format("YYYY-mm-dd HH:MM"));
                $('.form_datetime').datetimepicker('update');
            }            
            $('#runOnStart').prop('checked',data.RunOnStart);
            if(!$("#editRunParam").is(':visible')){
                $("#editRunParam").collapse('show');
            }
         });        
    };

    this.saveRunParam=function(){
        var self=this;
        ajax('EditTaskRunParam',{
            'id':$('#taskId').val(),
            'timerType':$('#timeType').val(),
            'interval':$('#interval').val(),
            'startTime':$('#startTime').val(),
            'runOnStart':$('#runOnStart').prop('checked')
        },function(data){
            $("#editRunParam").collapse('hide');
            self.getTasks();
        });
    }

    this.editConfig=function(id){
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
        $('.form_datetime').datetimepicker({
            language:  'zh-CN',
            weekStart: 0,
            todayBtn:  1,
            autoclose: 1,
            todayHighlight: 1,
            startView: 2,
            forceParse: 0,
            showMeridian:0,
            minuteStep:1
        });
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




