//@ sourceURL= mail.js
var Mail = new (function () {
    var ajax = function (method, datas, callback) {
        var url = method;
        if (datas) {
            var pa = '';
            for (var k in datas) {
                pa += k + '=' + datas[k] + '&'
            }
            url += '?' + pa.substr(0, pa.length - 1);
        }
        $.get(basepath + url, function (res) {
            if (res.State == 0) {
                alert(res.Msg);
                return;
            }
            if (callback)
                callback(res.Data)
        });
    };

    var post = function (method, datas, callback) {
        var url = method;
        $.post(basepath + url, datas, function (res) {
            if (res.State == 0) {
                alert(res.Msg);
                return;
            }
            if (callback)
                callback(res.Data)
        });
    };

    this.getData = function(){
        ajax('GetEmailAccount', null, function (res) {
            var data = JSON.parse(res);
            if(data.enable){
                $('#cbEnableEmail').prop('checked', true);
                $('#inputMailServer').val(data.smtpServer);
                $('#inputMailPort').val(data.smtpPort);
                $('#inputMailUserName').val(data.userName);
                $('#inputMailPassword').val(data.password);
                $('#inputMailContent').val(data.content);
                $("#divEmail").collapse("show");
            }
            else{
                $('#inputMailServer').val('');
                $('#inputMailPort').val('');
                $('#inputMailUserName').val('');
                $('#inputMailPassword').val('');
                $('#inputMailContent').val('');
            }
        });
    };

    this.enableMail=function(data){
        post('EnableEmail',data,function(res){
            alert('保存成功!');
        });
    };

    this.disableMail=function(){
        post('DisableEmail',null,function(res){
            alert('保存成功!');
        });
    };
    
})();
(function () {
    $(document).ready(function () {
        $("#cbEnableEmail").change(function (e) {
            if ($(e.target).prop('checked')) {
                $("#divEmail").collapse("show");
            }
            else {
                $("#divEmail").collapse("hide");
            }
        });
        $('#btnSaveMail').click(function(e){
            // var validator = $('#divEmail').data('bootstrapValidator');
            // validator.validate();
            // if (validator.isValid()) {
  
            // }
            if($('#cbEnableEmail').prop('checked')){
                let server=$('#inputMailServer').val();
                let port=$('#inputMailPort').val();
                let username=$('#inputMailUserName').val();
                let password=$('#inputMailPassword').val();               
                let content=$('#inputMailContent').val();
                if(server.length==0||username.length==0||password.length==0||port.length==0||content.length==0)
                    return;
                Mail.enableMail({'server':server,'port':port,'username':username,"password":password,'content':content});
            }
            else{
                Mail.disableMail();
            }
        });

        // $('#divEmail').bootstrapValidator({
        //     feedbackIcons: {
        //         valid: 'glyphicon glyphicon-ok',
        //         invalid: 'glyphicon glyphicon-remove',
        //         validating: 'glyphicon glyphicon-refresh'
        //     },
        //     submitButtons: '#btnSaveMail',
        //     fields: {
        //         inputMailServer: {
        //             validators: {
        //                 notEmpty: {
        //                     message: 'Smtp服务器不能为空！'
        //                 }    
        //             }
        //         },
        //         inputMailUserName: {
        //             validators: {
        //                 notEmpty: {
        //                     message: '邮箱账号不能为空。'
        //                 }
        //             }
        //         },
        //         inputMailPassword: {
        //             validators: {
        //                 notEmpty: {
        //                     message: '邮箱授权码不能为空。'
        //                 }
        //             }
        //         }
        //     }
        // });

        Mail.getData();
    });
})();

