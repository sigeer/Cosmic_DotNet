﻿using server.maps;

namespace Application.Core.Game.Maps
{
    public interface IMapObject
    {
        public IMap MapModel { get; }
        void setMap(IMap map);
        IMap getMap();
        Point getPosition();
        int getObjectId();
        void setObjectId(int id);
        MapObjectType getType();
        void setPosition(Point position);
        void sendSpawnData(IClient client);
        void sendDestroyData(IClient client);
        void nullifyPosition();
    }
}
