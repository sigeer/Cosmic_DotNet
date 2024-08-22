/*
	This file is part of the OdinMS Maple Story Server
    Copyright (C) 2008 Patrick Huy <patrick.huy@frz.cc>
		       Matthias Butz <matze@odinms.de>
		       Jan Christian Meyer <vimes@odinms.de>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation version 3 as published by
    the Free Software Foundation. You may not use, modify or distribute
    this program under any other version of the GNU Affero General Public
    License.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/


using client;
using net.packet;
using net.server;
using tools;

namespace server.maps;

/**
 * @author Matze
 * @author Ronan (HeavenMS)
 */
public class MiniGame : AbstractMapObject
{
    private Character owner;
    private Character visitor;
    private string password;
    private MiniGameType GameType = MiniGameType.UNDEFINED;
    private int piecetype;
    private int inprogress = 0;
    private int[] piece = new int[250];
    private List<int> list4x3 = new();
    private List<int> list5x4 = new();
    private List<int> list6x5 = new();
    private string description;
    private int loser = 1;
    private int firstslot = 0;
    private int visitorpoints = 0, visitorscore = 0, visitorforfeits = 0, lastvisitor = -1;
    private int ownerpoints = 0, ownerscore = 0, ownerforfeits = 0;
    private bool visitorquit, ownerquit;
    private long nextavailabletie = 0;
    private int matchestowin = 0;



    public enum MiniGameResult
    {
        WIN, LOSS, TIE
    }

    public MiniGame(Character owner, string description, string password)
    {
        this.owner = owner;
        this.description = description;
        this.password = password;
    }

    public string getPassword()
    {
        return this.password;
    }

    public bool checkPassword(string sentPw)
    {
        return this.password.Length == 0 || sentPw.ToLower().Equals(this.password.ToLower());
    }

    public bool hasFreeSlot()
    {
        return visitor == null;
    }

    public bool isOwner(Character chr)
    {
        return owner.Equals(chr);
    }

    public void addVisitor(Character challenger)
    {
        visitor = challenger;
        if (lastvisitor != challenger.getId())
        {
            ownerscore = 0;
            ownerforfeits = 0;

            visitorscore = 0;
            visitorforfeits = 0;
            lastvisitor = challenger.getId();
        }

        var owner = this.getOwner();
        if (GameType == MiniGameType.OMOK)
        {
            owner.sendPacket(PacketCreator.getMiniGameNewVisitor(this, challenger, 1));
            owner.getMap().broadcastMessage(PacketCreator.addOmokBox(owner, 2, 0));
        }
        else if (GameType == MiniGameType.MATCH_CARD)
        {
            owner.sendPacket(PacketCreator.getMatchCardNewVisitor(this, challenger, 1));
            owner.getMap().broadcastMessage(PacketCreator.addMatchCardBox(owner, 2, 0));
        }
    }

    public void closeRoom(bool forceClose)
    {
        owner.getMap().broadcastMessage(PacketCreator.removeMinigameBox(owner));

        if (forceClose)
        {
            this.broadcastToOwner(PacketCreator.getMiniGameClose(false, 4));
        }
        this.broadcastToVisitor(PacketCreator.getMiniGameClose(true, 3));

        if (visitor != null)
        {
            visitor.setMiniGame(null);
            visitor = null;
        }

        owner.setMiniGame(null);
        owner = null;
    }

    public void removeVisitor(bool forceClose, Character challenger)
    {
        if (visitor == challenger)
        {
            if (isMatchInProgress())
            { // owner is winner if visitor leave in progress
                minigameMatchOwnerWins(true);
            }
            if (forceClose)
            {
                visitor.sendPacket(PacketCreator.getMiniGameClose(true, 4));
            }

            challenger.setMiniGame(null);
            visitor = null;

            this.getOwner().sendPacket(PacketCreator.getMiniGameRemoveVisitor());
            if (GameType == MiniGameType.OMOK)
            {
                this.getOwner().getMap().broadcastMessage(PacketCreator.addOmokBox(owner, 1, 0));
            }
            else if (GameType == MiniGameType.MATCH_CARD)
            {
                this.getOwner().getMap().broadcastMessage(PacketCreator.addMatchCardBox(owner, 1, 0));
            }
        }
    }

    public bool isVisitor(Character challenger)
    {
        return visitor == challenger;
    }

    public void broadcastToOwner(Packet packet)
    {
        Client c = owner.getClient();
        if (c != null)
        {
            c.sendPacket(packet);
        }
    }

    public void broadcastToVisitor(Packet packet)
    {
        if (visitor != null)
        {
            visitor.sendPacket(packet);
        }
    }

    public void setFirstSlot(int type)
    {
        firstslot = type;
    }

    public int getFirstSlot()
    {
        return firstslot;
    }

    private void updateMiniGameBox()
    {
        this.getOwner().getMap().broadcastMessage(PacketCreator.addOmokBox(owner, visitor != null ? 2 : 1, inprogress));
    }

    object finishCheckLock = new object();
    private bool minigameMatchFinish()
    {
        lock (finishCheckLock)
        {
            if (isMatchInProgress())
            {
                inprogress = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void minigameMatchFinished()
    {
        updateMiniGameBox();

        if (ownerquit)
        {
            owner.closeMiniGame(true);
        }
        else if (visitorquit)
        {
            visitor.closeMiniGame(true);
        }
    }

    public void minigameMatchStarted()
    {
        inprogress = 1;
        ownerquit = false;
        visitorquit = false;
    }

    public void setQuitAfterGame(Character player, bool quit)
    {
        if (isOwner(player))
        {
            ownerquit = quit;
        }
        else
        {
            visitorquit = quit;
        }
    }

    public bool isMatchInProgress()
    {
        return inprogress != 0;
    }

    public void denyTie(Character chr)
    {
        if (this.isOwner(chr))
        {
            inprogress |= (1 << 1);
        }
        else
        {
            inprogress |= (1 << 2);
        }
    }

    public bool isTieDenied(Character chr)
    {
        if (this.isOwner(chr))
        {
            return ((inprogress >> 2) % 2) == 1;
        }
        else
        {
            return ((inprogress >> 1) % 2) == 1;
        }
    }

    public void minigameMatchOwnerWins(bool forfeit)
    {
        if (!minigameMatchFinish())
        {
            return;
        }

        owner.setMiniGamePoints(visitor, 1, this.isOmok());

        if (visitorforfeits < 4 || !forfeit)
        {
            ownerscore += 50;
        }
        visitorscore += (15 * (forfeit ? -1 : 1));
        if (forfeit)
        {
            visitorforfeits++;
        }

        this.broadcast(PacketCreator.getMiniGameOwnerWin(this, forfeit));

        minigameMatchFinished();
    }

    public void minigameMatchVisitorWins(bool forfeit)
    {
        if (!minigameMatchFinish())
        {
            return;
        }

        owner.setMiniGamePoints(visitor, 2, this.isOmok());

        if (ownerforfeits < 4 || !forfeit)
        {
            visitorscore += 50;
        }
        ownerscore += (15 * (forfeit ? -1 : 1));
        if (forfeit)
        {
            ownerforfeits++;
        }

        this.broadcast(PacketCreator.getMiniGameVisitorWin(this, forfeit));

        minigameMatchFinished();
    }

    public void minigameMatchDraw()
    {
        if (!minigameMatchFinish())
        {
            return;
        }

        owner.setMiniGamePoints(visitor, 3, this.isOmok());

        long timeNow = Server.getInstance().getCurrentTime();
        if (nextavailabletie <= timeNow)
        {
            visitorscore += 10;
            ownerscore += 10;

            nextavailabletie = timeNow + 5 * 60 * 1000;
        }

        this.broadcast(PacketCreator.getMiniGameTie(this));

        minigameMatchFinished();
    }

    public void setOwnerPoints()
    {
        ownerpoints++;
        if (ownerpoints + visitorpoints == matchestowin)
        {
            if (ownerpoints == visitorpoints)
            {
                minigameMatchDraw();
            }
            else if (ownerpoints > visitorpoints)
            {
                minigameMatchOwnerWins(false);
            }
            else
            {
                minigameMatchVisitorWins(false);
            }
            ownerpoints = 0;
            visitorpoints = 0;
        }
    }

    public void setVisitorPoints()
    {
        visitorpoints++;
        if (ownerpoints + visitorpoints == matchestowin)
        {
            if (ownerpoints > visitorpoints)
            {
                minigameMatchOwnerWins(false);
            }
            else if (visitorpoints > ownerpoints)
            {
                minigameMatchVisitorWins(false);
            }
            else
            {
                minigameMatchDraw();
            }
            ownerpoints = 0;
            visitorpoints = 0;
        }
    }

    public void setMatchesToWin(int type)
    {
        matchestowin = type;
    }

    public void setPieceType(int type)
    {
        piecetype = type;
    }

    public int getPieceType()
    {
        return piecetype;
    }

    public void setGameType(MiniGameType game)
    {
        GameType = game;
        if (GameType == MiniGameType.MATCH_CARD)
        {
            if (matchestowin == 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    list4x3.Add(i);
                    list4x3.Add(i);
                }
            }
            else if (matchestowin == 10)
            {
                for (int i = 0; i < 10; i++)
                {
                    list5x4.Add(i);
                    list5x4.Add(i);
                }
            }
            else
            {
                for (int i = 0; i < 15; i++)
                {
                    list6x5.Add(i);
                    list6x5.Add(i);
                }
            }
        }
    }

    public MiniGameType getGameType()
    {
        return GameType;
    }

    public bool isOmok()
    {
        return GameType.Equals(MiniGameType.OMOK);
    }

    public void shuffleList()
    {
        if (matchestowin == 6)
        {
            Collections.shuffle(list4x3);
        }
        else if (matchestowin == 10)
        {
            Collections.shuffle(list5x4);
        }
        else
        {
            Collections.shuffle(list6x5);
        }
    }

    public int getCardId(int slot)
    {
        int cardid;
        if (matchestowin == 6)
        {
            cardid = list4x3.get(slot);
        }
        else if (matchestowin == 10)
        {
            cardid = list5x4.get(slot);
        }
        else
        {
            cardid = list6x5.get(slot);
        }
        return cardid;
    }

    public int getMatchesToWin()
    {
        return matchestowin;
    }

    public void setLoser(int type)
    {
        loser = type;
    }

    public int getLoser()
    {
        return loser;
    }

    public void broadcast(Packet packet)
    {
        broadcastToOwner(packet);
        broadcastToVisitor(packet);
    }

    public void chat(Client c, string chat)
    {
        broadcast(PacketCreator.getPlayerShopChat(c.getPlayer(), chat, isOwner(c.getPlayer())));
    }

    public void sendOmok(Client c, int type)
    {
        c.sendPacket(PacketCreator.getMiniGame(c, this, isOwner(c.getPlayer()), type));
    }

    public void sendMatchCard(Client c, int type)
    {
        c.sendPacket(PacketCreator.getMatchCard(c, this, isOwner(c.getPlayer()), type));
    }

    public Character getOwner()
    {
        return owner;
    }

    public Character getVisitor()
    {
        return visitor;
    }

    public void setPiece(int move1, int move2, int type, Character chr)
    {
        int slot = move2 * 15 + move1 + 1;
        if (piece[slot] == 0)
        {
            piece[slot] = type;
            this.broadcast(PacketCreator.getMiniGameMoveOmok(this, move1, move2, type));
            for (int y = 0; y < 15; y++)
            {
                for (int x = 0; x < 11; x++)
                {
                    if (searchCombo(x, y, type))
                    {
                        if (this.isOwner(chr))
                        {
                            this.minigameMatchOwnerWins(false);
                            this.setLoser(0);
                        }
                        else
                        {
                            this.minigameMatchVisitorWins(false);
                            this.setLoser(1);
                        }
                        for (int y2 = 0; y2 < 15; y2++)
                        {
                            for (int x2 = 0; x2 < 15; x2++)
                            {
                                int slot2 = (y2 * 15 + x2 + 1);
                                piece[slot2] = 0;
                            }
                        }
                    }
                }
            }
            for (int y = 0; y < 15; y++)
            {
                for (int x = 4; x < 15; x++)
                {
                    if (searchCombo2(x, y, type))
                    {
                        if (this.isOwner(chr))
                        {
                            this.minigameMatchOwnerWins(false);
                            this.setLoser(0);
                        }
                        else
                        {
                            this.minigameMatchVisitorWins(false);
                            this.setLoser(1);
                        }
                        for (int y2 = 0; y2 < 15; y2++)
                        {
                            for (int x2 = 0; x2 < 15; x2++)
                            {
                                int slot2 = (y2 * 15 + x2 + 1);
                                piece[slot2] = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    private bool searchCombo(int x, int y, int type)
    {
        int slot = y * 15 + x + 1;
        for (int i = 0; i < 5; i++)
        {
            if (piece[slot + i] == type)
            {
                if (i == 4)
                {
                    return true;
                }
            }
            else
            {
                break;
            }
        }
        for (int j = 15; j < 17; j++)
        {
            for (int i = 0; i < 5; i++)
            {
                if (piece[slot + i * j] == type)
                {
                    if (i == 4)
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        return false;
    }

    private bool searchCombo2(int x, int y, int type)
    {
        int slot = y * 15 + x + 1;
        for (int j = 14; j < 15; j++)
        {
            for (int i = 0; i < 5; i++)
            {
                if (piece[slot + i * j] == type)
                {
                    if (i == 4)
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        return false;
    }

    public string getDescription()
    {
        return description;
    }

    public int getOwnerScore()
    {
        return ownerscore;
    }

    public int getVisitorScore()
    {
        return visitorscore;
    }

    public override void sendDestroyData(Client client) { }

    public override void sendSpawnData(Client client) { }

    public override MapObjectType getType()
    {
        return MapObjectType.MINI_GAME;
    }
}


public enum MiniGameType
{
    UNDEFINED = 0, OMOK = 1, MATCH_CARD = 2
}