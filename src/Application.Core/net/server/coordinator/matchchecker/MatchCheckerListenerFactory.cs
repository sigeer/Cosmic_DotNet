/*
    This file is part of the HeavenMS MapleStory Server
    Copyleft (L) 2016 - 2019 RonanLana

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


using Application.Core.net.server.coordinator.matchchecker.listener;

namespace net.server.coordinator.matchchecker;

/**
 * @author Ronan
 */
public class MatchCheckerType
{

    public static readonly MatchCheckerType GUILD_CREATION = new(new MatchCheckerGuildCreationListener());
    public static readonly MatchCheckerType CPQ_CHALLENGE = new(new MatchCheckerCPQChallengeListener());

    private AbstractMatchCheckerListener listener;

    private MatchCheckerType(AbstractMatchCheckerListener listener)
    {
        this.listener = listener;
    }

    public AbstractMatchCheckerListener getListener()
    {
        return this.listener;
    }
}