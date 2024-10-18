#### 脚本相关

 - Channel 和 EventScriptManager 是一对一

 - EventScriptManager 和 EventManager（em） 是一对多，在服务器启动时生成，直接装载了所有event下的脚本（EventManager与event脚本一对一），并调用了init方法。
EventManager一般在js中通过`newInstance`，创建 EventInstanceManager（eim） ，可以将`EventInstanceManager`视作`EventManager`的实例，一对多

- NPCScriptManager 是单例，当与NPC对话时，调用`start`方法开启对话，同时生成 NPCConversationManager（cm）。
之后调用`action`，通过参数进入对话的不同阶段


#### 副本相关

##### 月妙

`/event/HenesysPQ.js`
入口：`/npc/1012112.js`

##### 废都

`/event/KerningPQ.js`
入口：`/npc/9020000.js`
过关NPC：`/npc/9020001.js`


##### 101层

`/event/LudiPQ.js`
入口：`/npc/2040034.js`
过关NPC：`/npc/2040036.js`~`/npc/2040044.js`

##### 艾琳森林

`/event/EllinPQ.js`
入口：`/npc/2133000.js`
传送点过关，脚本`/portal/party6_*.js`
部分关卡NPC：`/npc/2133001.js` 
跳跳关卡NPC：`/npc/2133004.js`
奖励：`/portal/party6_stage800.js`