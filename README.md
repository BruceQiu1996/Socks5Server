>一个基于.net 7的高性能socks5代理服务端与客户端
#### 1.服务端如何使用？
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
#### 2.客户端使用
##### 2.1 主界面
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/clientmainpage.jpg)
##### 2.2 配置界面
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/settings.jpg)
服务端代码解释博客:https://www.cnblogs.com/qwqwQAQ/p/17410319.html


