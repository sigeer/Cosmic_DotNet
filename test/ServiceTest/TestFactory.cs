﻿using Application.Core.Game;
using Application.Core.Managers;
using net.server;

namespace ServiceTest
{
    public class TestFactory
    {
        public static IClient GenerateTestClient()
        {
            Server.getInstance().forceUpdateCurrentTime();
            var mockClient = Client.createMock();
            mockClient.setPlayer(CharacterManager.GetPlayerById(1));
            return mockClient;
        }
    }
}
