using Application.Core.Game.Life;
using Application.Core.Game.TheWorld;
using Application.Core.Scripting.Infrastructure;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using net.packet;
using net.server;
using net.server.channel;
using net.server.coordinator.session;
using scripting;
using scripting.Event;
using scripting.npc;
using scripting.quest;

namespace Application.Core.Game
{
    public class MockupClient : IClient
    {
        IPlayer? mockPlayer;
        public IPlayer? Character => mockPlayer;

        public string ClientInfo => "MockupClient";
        int _world;
        int _channel;

        public int World { get; set; }
        public int Channel { get; set; }

        public MockupClient(int world, int channel)
        {
            _world = world;
            _channel = channel;

            World = world;
            Channel = channel;
        }

        public MockupClient() : this(0, 1) { }

        public bool acceptToS()
        {
            throw new NotImplementedException();
        }

        public void addVotePoints(int points)
        {
            throw new NotImplementedException();
        }

        public void announceBossHpBar(Monster mm, int mobHash, Packet packet)
        {
            throw new NotImplementedException();
        }

        public void announceHint(string msg, int length)
        {
            throw new NotImplementedException();
        }

        public void announceServerMessage()
        {
            throw new NotImplementedException();
        }

        public bool attemptCsCoupon()
        {
            throw new NotImplementedException();
        }

        public void banHWID()
        {
            throw new NotImplementedException();
        }

        public void banMacs()
        {
            throw new NotImplementedException();
        }

        public bool canBypassPic()
        {
            throw new NotImplementedException();
        }

        public bool canBypassPin()
        {
            throw new NotImplementedException();
        }

        public bool canClickNPC()
        {
            throw new NotImplementedException();
        }

        public bool canGainCharacterSlot()
        {
            throw new NotImplementedException();
        }

        public bool canRequestCharlist()
        {
            throw new NotImplementedException();
        }

        public void changeChannel(int channel)
        {
            throw new NotImplementedException();
        }

        public bool checkBirthDate(DateTime date)
        {
            throw new NotImplementedException();
        }

        public void checkChar(int accid)
        {
            throw new NotImplementedException();
        }

        public void checkIfIdle(IdleStateEvent evt)
        {
            throw new NotImplementedException();
        }

        public bool checkPic(string other)
        {
            throw new NotImplementedException();
        }

        public bool checkPin(string other)
        {
            throw new NotImplementedException();
        }

        public void closePlayerScriptInteractions()
        {
            return;
        }

        public void closeSession()
        {
            throw new NotImplementedException();
        }

        public bool deleteCharacter(int cid, int senderAccId)
        {
            throw new NotImplementedException();
        }

        public void disconnect(bool shutdown, bool cashshop)
        {
            return;
        }

        public void disconnectSession()
        {
            throw new NotImplementedException();
        }

        public void enableCSActions()
        {
            throw new NotImplementedException();
        }

        public void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
        {
            throw new NotImplementedException();
        }

        public int finishLogin()
        {
            throw new NotImplementedException();
        }

        public void forceDisconnect()
        {
            throw new NotImplementedException();
        }

        public bool gainCharacterSlot()
        {
            throw new NotImplementedException();
        }

        public AbstractPlayerInteraction getAbstractPlayerInteraction()
        {
            throw new NotImplementedException();
        }

        public int getAccID()
        {
            throw new NotImplementedException();
        }

        public string getAccountName()
        {
            throw new NotImplementedException();
        }

        public short getAvailableCharacterSlots()
        {
            throw new NotImplementedException();
        }

        public short getAvailableCharacterWorldSlots()
        {
            throw new NotImplementedException();
        }

        public short getAvailableCharacterWorldSlots(int world)
        {
            throw new NotImplementedException();
        }

        public int getChannel()
        {
            return 0;
        }
        public IWorldChannel getChannelServer()
        {
            return getWorldServer().getChannel(_channel);
        }

        public IWorldChannel getChannelServer(byte channel)
        {
            return getWorldServer().getChannel(channel);
        }

        public short getCharacterSlots()
        {
            throw new NotImplementedException();
        }

        public NPCConversationManager? getCM()
        {
            throw new NotImplementedException();
        }

        public EventManager? getEventManager(string evt)
        {
            throw new NotImplementedException();
        }

        public sbyte getGender()
        {
            throw new NotImplementedException();
        }

        public int getGMLevel()
        {
            throw new NotImplementedException();
        }

        public sbyte getGReason()
        {
            throw new NotImplementedException();
        }

        public Hwid getHwid()
        {
            throw new NotImplementedException();
        }

        public int getLanguage()
        {
            throw new NotImplementedException();
        }

        public long getLastPacket()
        {
            throw new NotImplementedException();
        }

        public int getLoginState()
        {
            throw new NotImplementedException();
        }

        public HashSet<string> getMacs()
        {
            throw new NotImplementedException();
        }

        public string getPic()
        {
            throw new NotImplementedException();
        }

        public string getPin()
        {
            throw new NotImplementedException();
        }

        public IPlayer? getPlayer()
        {
            throw new NotImplementedException();
        }

        public QuestActionManager? getQM()
        {
            throw new NotImplementedException();
        }

        public string getRemoteAddress()
        {
            throw new NotImplementedException();
        }

        public IEngine? getScriptEngine(string name)
        {
            throw new NotImplementedException();
        }

        public long getSessionId()
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset? getTempBanCalendar()
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset? getTempBanCalendarFromDB()
        {
            throw new NotImplementedException();
        }

        public int getVisibleWorlds()
        {
            throw new NotImplementedException();
        }

        public int getVotePoints()
        {
            throw new NotImplementedException();
        }

        public int getWorld()
        {
            return _world;
        }

        IWorld? _worldServer;
        public IWorld getWorldServer()
        {
            if (_worldServer == null)
                _worldServer = Server.getInstance().getWorld(Server.getInstance().addWorld());
            return _worldServer;
        }

        public bool hasBannedHWID()
        {
            throw new NotImplementedException();
        }

        public bool hasBannedIP()
        {
            throw new NotImplementedException();
        }

        public bool hasBannedMac()
        {
            throw new NotImplementedException();
        }

        public bool hasBeenBanned()
        {
            throw new NotImplementedException();
        }

        public bool isInTransition()
        {
            throw new NotImplementedException();
        }

        public bool isLoggedIn()
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> loadCharacters(int serverId)
        {
            throw new NotImplementedException();
        }

        public void lockClient()
        {
            throw new NotImplementedException();
        }

        public int login(string login, string pwd, Hwid nibbleHwid)
        {
            throw new NotImplementedException();
        }

        public void pongReceived()
        {
            throw new NotImplementedException();
        }

        public void releaseClient()
        {
            throw new NotImplementedException();
        }

        public void removeClickedNPC()
        {
            throw new NotImplementedException();
        }

        public void removeScriptEngine(string name)
        {
            throw new NotImplementedException();
        }

        public void requestedServerlist(int worlds)
        {
            throw new NotImplementedException();
        }

        public void resetCsCoupon()
        {
            throw new NotImplementedException();
        }

        public void sendCharList(int server)
        {
            throw new NotImplementedException();
        }

        public void sendPacket(Packet packet)
        {
            Console.WriteLine("SendPacket");
            return;
        }

        public void setAccID(int id)
        {
            throw new NotImplementedException();
        }

        public void setAccountName(string a)
        {
            throw new NotImplementedException();
        }

        public void setChannel(int channel)
        {
            throw new NotImplementedException();
        }

        public void setCharacterOnSessionTransitionState(int cid)
        {
            throw new NotImplementedException();
        }

        public void setCharacterSlots(sbyte slots)
        {
            throw new NotImplementedException();
        }

        public void setClickedNPC()
        {
            throw new NotImplementedException();
        }

        public void setGender(sbyte m)
        {
            throw new NotImplementedException();
        }

        public void setGMLevel(int level)
        {
            throw new NotImplementedException();
        }

        public void setHwid(Hwid? hwid)
        {
            throw new NotImplementedException();
        }

        public void setLanguage(int lingua)
        {
            throw new NotImplementedException();
        }

        public void setPic(string pic)
        {
            throw new NotImplementedException();
        }

        public void setPin(string pin)
        {
            throw new NotImplementedException();
        }

        public void setPlayer(IPlayer? player)
        {
            mockPlayer = player;
        }

        public void setScriptEngine(string name, IEngine e)
        {
            throw new NotImplementedException();
        }

        public void setWorld(int world)
        {
            throw new NotImplementedException();
        }

        public bool tryacquireClient()
        {
            throw new NotImplementedException();
        }

        public bool tryacquireEncoder()
        {
            throw new NotImplementedException();
        }

        public void unlockClient()
        {
            throw new NotImplementedException();
        }

        public void unlockEncoder()
        {
            throw new NotImplementedException();
        }

        public void updateHwid(Hwid hwid)
        {
            throw new NotImplementedException();
        }

        public void updateLastPacket()
        {
            throw new NotImplementedException();
        }

        public void updateLoginState(int newState)
        {
            throw new NotImplementedException();
        }

        public void updateMacs(string macData)
        {
            throw new NotImplementedException();
        }

        public void UserEventTriggered(IChannelHandlerContext ctx, object evt)
        {
            throw new NotImplementedException();
        }

        public void useVotePoints(int points)
        {
            throw new NotImplementedException();
        }
    }
}
