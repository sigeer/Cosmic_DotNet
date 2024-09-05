﻿using client;
using constants.id;
using server;
using tools;

namespace Application.Core.Game.Players
{
    public partial class Player
    {
        private int marriageItemid = -1;
        private Ring? marriageRing = null;

        private int partnerId = -1;
        public int getPartnerId()
        {
            return partnerId;
        }

        public void setPartnerId(int partnerid)
        {
            partnerId = partnerid;
        }

        public int getMarriageItemId()
        {
            return marriageItemid;
        }

        public void setMarriageItemId(int itemid)
        {
            marriageItemid = itemid;
        }

        public bool isMarried()
        {
            return marriageRing != null && partnerId > 0;
        }

        public bool haveWeddingRing()
        {
            int[] rings = { ItemId.WEDDING_RING_STAR, ItemId.WEDDING_RING_MOONSTONE, ItemId.WEDDING_RING_GOLDEN, ItemId.WEDDING_RING_SILVER };

            foreach (int ringid in rings)
            {
                if (haveItemWithId(ringid, true))
                {
                    return true;
                }
            }

            return false;
        }
        public Ring? getMarriageRing()
        {
            return partnerId > 0 ? marriageRing : null;
        }

        public Marriage? getMarriageInstance()
        {
            return getEventInstance() as Marriage;
        }

        public Ring? getRingById(int id)
        {
            foreach (Ring ring in getCrushRings())
            {
                if (ring.getRingId() == id)
                {
                    return ring;
                }
            }
            foreach (Ring ring in getFriendshipRings())
            {
                if (ring.getRingId() == id)
                {
                    return ring;
                }
            }

            if (marriageRing != null)
            {
                if (marriageRing.getRingId() == id)
                {
                    return marriageRing;
                }
            }

            return null;
        }

        public void addMarriageRing(Ring? r)
        {
            marriageRing = r;
        }

        public bool hasJustMarried()
        {
            var eim = getEventInstance();
            if (eim != null)
            {
                var prop = eim.getProperty("groomId");

                if (prop != null)
                {
                    return (int.Parse(prop) == Id || eim.getIntProperty("brideId") == Id) &&
                            (Map == MapId.CHAPEL_WEDDING_ALTAR || Map == MapId.CATHEDRAL_WEDDING_ALTAR);
                }
            }

            return false;
        }

        public void broadcastMarriageMessage()
        {
            var guild = this.getGuild();
            if (guild != null)
            {
                guild.broadcast(PacketCreator.marriageMessage(0, Name));
            }

            var family = this.getFamily();
            if (family != null)
            {
                family.broadcast(PacketCreator.marriageMessage(1, Name));
            }
        }

        public void CheckMarriageData()
        {
            if (MarriageItemId > 0 && PartnerId <= 0)
            {
                MarriageItemId = -1;
            }
            else if (PartnerId > 0 && getWorldServer().getRelationshipId(Id) <= 0)
            {
                MarriageItemId = -1;
                PartnerId = -1;
            }
        }
    }
}
