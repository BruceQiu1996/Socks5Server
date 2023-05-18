>一个基于.net 7的高性能socks5代理服务端
#### 1.如何使用？
##### 1.1 配置服务端
appsettings.json:
```
  "ServerConfig": {
    "Port": 8333,//服务端端口
    "NeedAuth": true,//是否启动认证
    "AuthVersion": 1
  }
```
##### 1.2 还原数据库结构
使用efcore命令还原数据库架构
```
安装efcore tool
dotnet tool install --global dotnet-ef
还原更新数据库
dotnet ef database update
```
默认管理员用户名密码：admin/123456
#### 2.使用
##### 2.1 启动服务端

##### 2.2 使用代理软件如QQ或者Proxifier配置代理
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/proxifierconfig.jpg)
##### 2.3 查看流量走向
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/prodisplay.jpg)
##### 2.4一个简单的用户管理界面
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/manmain.jpg)

代码解释博客:https://www.cnblogs.com/qwqwQAQ/p/17410319.html


