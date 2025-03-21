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


using Application.Core.Scripting.Infrastructure;
using server.maps;

namespace scripting.portal;



public class PortalScriptManager : AbstractScriptManager
{
    private static PortalScriptManager instance = new PortalScriptManager();

    readonly EngineStorage _scripts = new EngineStorage();

    public static PortalScriptManager getInstance()
    {
        return instance;
    }

    private IEngine? getPortalScript(string scriptName)
    {
        string scriptPath = GetPortalScriptPath(scriptName);
        var script = _scripts[scriptPath];
        if (script != null)
        {
            return script;
        }

        var engine = getInvocableScriptEngine(scriptPath);
        if (engine == null)
        {
            return null;
        }

        _scripts[scriptPath] = engine;
        return script;
    }

    public bool executePortalScript(Portal portal, IClient c)
    {
        try
        {
            var script = getPortalScript(portal.getScriptName());
            if (script != null)
            {
                return script.CallFunction("enter", new PortalPlayerInteraction(c, portal)).ToObject<bool>();
            }
        }
        catch (Exception e)
        {
            log.Error(e, "Portal script error in: {ScriptName}", portal.getScriptName());
        }
        return false;
    }

    public void reloadPortalScripts()
    {
        _scripts.Clear();
    }
}