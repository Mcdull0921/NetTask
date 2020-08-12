//@ sourceURL= user.js
var User = new (function () {
    var self=this;
    self.isEdit=false;
 this.addUser=function() {
    if($("#divAddUser").is(':visible')){
        return;
    }
    $('#divPassword').css('display','');
    $("#inputUserName").prop('disabled','');
    $("#inputPassword").prop('disabled',''); 
    $("#inputPassword").val("");
    $("#inputUserName").val("");
    $('input[type="radio"]')[0].checked=true;
    $('#cbReceiveEmail').prop('checked', false);
    $('#inputEmail').val('');
    reloadValidator();
    $("#divAddUser").collapse('show');
    self.isEdit=false;
}

this.editUser=function(username) {
    if($("#divAddUser").is(':visible')){
        return;
    }
    var user=self.users[username];
    $('#divPassword').css('display','none');
    $("#inputUserName").val(user.userName);
    $("#inputUserName").prop('disabled','disabled');
    $("#inputPassword").val('********');
    $("#inputPassword").prop('disabled','disabled'); 
    $('input[type="radio"]')[user.role].checked=true;
    $('#cbReceiveEmail').prop('checked', user.receiveEmail);
    $('#inputEmail').val(user.email);  
    reloadValidator();
    $("#divAddUser").collapse('show');
    self.isEdit=true;
}



this.addUser_submit=function() {
    var validator = $('#divAddUser').data('bootstrapValidator');
    validator.validate();
    //alert(validator.isValid());
    var callback=function (res) {
        if (res.State == 0) {
            alert("保存失败：" + res.Msg);
            return;
        }
        $("#divAddUser").collapse('hide');
        self.selectUsers();
        $("#inputPassword").val("");
        $("#inputUserName").val("");
        $('input[type="radio"]')[0].checked=true;
        $('#cbReceiveEmail').prop('checked', false);
        $('#inputEmail').val('');
    };
    if (validator.isValid()) {
        var url=basepath +"AddUserV2?username=" +
        $("#inputUserName").val() +
        "&userpwd=" +
        $("#inputPassword").val() +
        "&role=" + $('input[type="radio"]:checked').val()+
        '&receiveEmail='+$('#cbReceiveEmail').prop('checked')+
        '&email='+$('#inputEmail').val();
        if(self.isEdit){
            url=basepath +"EditUser?username=" +
        $("#inputUserName").val() +
        "&role=" + $('input[type="radio"]:checked').val()+
        '&receiveEmail='+$('#cbReceiveEmail').prop('checked')+
        '&email='+$('#inputEmail').val();
        }
        $.get(url,callback);
    }
}


this. delOneUser=function(userIndex, userName) {
    if (!confirm('是否删除'+userName)) {
        return;
    }

    $.get(basepath + "RemoveUser?id=" + userIndex + '&usernames=' + userName, function (res) {
        if (res.State == 0) {
            alert("操作失败：" + res.Msg);
            return;
        }
        self.selectUsers();
    });
}

this.selectUsers=function() {
    self.users={};
    $.get(basepath + "GetUsers", function (res) {
        var data = res.Data;
        var htmlStr = "";
        var htmlIsBanned = "<span data-feather='zap-off' color='red'></span> ";
        var htmlIsConnected = "<span data-feather='activity' color='green'></span> ";
        var htmlIsAdmin = "<span data-feather='star' color='orange'></span> ";
        for (var i=0; i< data.length;i++) {
            var user = $.parseJSON(data[i]);
            self.users[user.userName] = user;
            htmlStr += "<tr>" +
                //"<td> <input type='checkbox' style='zoom:150%;' name='cbxUserIds' value='" + i + "'></td>" +
                "<td>" + (i + 1) + "</td>" +
                "<td class='td_userid'>" + user.userId + "</td>" +
                "<td class='td_username'>" + user.userName + "</td>" +
                "<td>" + new Date(user.regTime).format("YYYY-mm-dd HH:MM") + "</td>";
            htmlStr += "<td>" + ['普通用户', '管理员', '超级管理员'][user.role] + "</td>";
            htmlStr+="<td>" + (user.email?user.email:'') + "</td>";
            htmlStr+="<td>"+(user.receiveEmail?"是":"否")+"</td>";
            var operate = ' <div class="btn-group" role="group">'
            +'<button type="button" class="btn btn-sm btn-outline-secondary" onclick="User.delOneUser('+i+',\''+user.userName+'\')"><span data-feather="user-x"></span> 删除</button>'
            +'<button type="button" class="btn btn-sm btn-outline-secondary" onclick="User.editUser(\''+user.userName+'\')"><span data-feather="edit"></span> 修改</button>'
            +'<button type="button" class="btn btn-sm btn-outline-secondary mr-2" onclick="User.resetPwd(\''+user.userName+'\')"><span data-feather="key"></span> 重置密码</button>'
            +'</div>';
            htmlStr += "<td class='td-ports'>" + operate + "</td>" +
                "</tr>";
        }
        $("#user_tb_body").html(htmlStr);
        if (feather)
            feather.replace();

    });
}


   this.resetPwd=function(username) {
    var pwd = prompt("请输入用户"+username+"的新密码：");
    if (pwd == null) {
        return;
    }
    if(pwd==''){
        alert("登录密码不能为空。");
        return false;
    }
    if (!/^[a-zA-Z0-9_\.]+$/.test(pwd)) {
        alert("密码格式错误，只支持数字和英文字符。");
        return;
    }
    $.get(basepath + "ResetPwd?userName=" + username + "&userPwd=" + pwd, function (res) {
        if (res.State == 0) {
            alert("密码重置失败：" + res.Msg);
            return;
        }
        alert('密码重置成功');
    });
}

function reloadValidator(){
    $("#divAddUser").data('bootstrapValidator').destroy();
    $('#divAddUser').data('bootstrapValidator', null);
    self.initValidate();
}

this.initValidate=function() {
    $('#divAddUser').bootstrapValidator({
        feedbackIcons: {
            valid: 'glyphicon glyphicon-ok',
            invalid: 'glyphicon glyphicon-remove',
            validating: 'glyphicon glyphicon-refresh'
        },
        submitButtons: '#btnAddUser',
        fields: {
            inputUserName: {
                validators: {
                    notEmpty: {
                        message: '用户登录名不能为空！'
                    },
                    regexp: {
                        regexp: /^[^0-9]+/,
                        message: '用户名不能以数字开头。'
                    },
                    remote: {
                        delay: 2000,
                        url: basepath + 'ValidateUserName',
                        type: "GET",
                        message: '用户名已经存在。',
                        data: function (validator) {
                            return {
                                p1: self.isEdit?'1':'0',//is edit
                                p2: $("#inputUserName").val()//old user
                                //p2: $("#inputUserName").val(),//new user
                            };
                        }
                    }

                }
            },
            inputPassword: {                
                validators: {
                    notEmpty: {
                        message: '登录密码不能为空。'
                    },
                    regexp:
                    {
                        regexp: /^[a-zA-Z0-9_\.]+$/,
                        message: '密码格式错误，只支持数字和英文字符。'
                    }
                }
            },
            inputEmail:{
                validators: {
                    regexp:
                    {
                        regexp: /^([a-zA-Z0-9]+[_|\_|\.]?)*[a-zA-Z0-9]+@([a-zA-Z0-9]+[_|\_|\.]?)*[a-zA-Z0-9]+\.[a-zA-Z]{2,3}$/,
                        message: '邮箱格式有误。'
                    }
                }
            }
        }
    });
}
})();

(function () {
    User.selectUsers();
    $(document).ready(function () {
        User.initValidate();
        document.getElementById("btnAddUser").onclick = function () { User.addUser_submit(); };
    });

})();