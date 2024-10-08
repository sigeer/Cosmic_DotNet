

using provider.wz;
using tools;

namespace tools.mapletools;

















/**
 * @author RonanLana
 * <p>
 * This application gets info from the WZ.XML files regarding cash itemids then searches the drop data on the DB
 * after any NX (cash item) drops and reports them.
 * <p>
 * Estimated parse time: 2 minutes
 */
public class CashDropFetcher {
    private static Path OUTPUT_FILE = ToolConstants.getOutputFile("cash_drop_report.txt");
    private static Connection con = SimpleDatabaseConnection.getConnection();
    private static int INITIAL_STRING_LENGTH = 50;
    private static int ITEM_FILE_NAME_SIZE = 13;

    private static HashSet<int> nxItems = new ();
    private static HashSet<int> nxDrops = new ();

    private static PrintWriter printWriter = null;
    private static BufferedReader bufferedReader = null;

    private static byte status = 0;
    private static int currentItemid = 0;

    private static string getName(string token) {
        int i, j;
        char[] dest;
        string d;

        i = token.lastIndexOf("name");
        i = token.indexOf("\"", i) + 1; //lower bound of the string
        j = token.indexOf("\"", i);     //upper bound

        if (j < i) {
            return "0";           //node value containing 'name' in it's scope, cheap fix since we don't deal with strings anyway
        }

        dest = new char[INITIAL_STRING_LENGTH];
        token.getChars(i, j, dest, 0);

        d = new string(dest);
        return (d.trim());
    }

    private static string getValue(string token) {
        int i, j;
        char[] dest;
        string d;

        i = token.lastIndexOf("value");
        i = token.indexOf("\"", i) + 1; //lower bound of the string
        j = token.indexOf("\"", i);     //upper bound

        dest = new char[INITIAL_STRING_LENGTH];
        token.getChars(i, j, dest, 0);

        d = new string(dest);
        return (d.trim());
    }

    private static void forwardCursor(int st) {
        string line = null;

        try {
            while (status >= st && (line = bufferedReader.readLine()) != null) {
                simpleToken(line);
            }
        } catch (Exception e) {
            Log.Logger.Error(e.ToString());
        }
    }

    private static void simpleToken(string token) {
        if (token.Contains("/imgdir")) {
            status -= 1;
        } else if (token.Contains("imgdir")) {
            status += 1;
        }
    }


    private static void inspectEquipWzEntry() {
        string line = null;

        try {
            while ((line = bufferedReader.readLine()) != null) {
                translateEquipToken(line);
            }
        } catch (Exception e) {
            Log.Logger.Error(e.ToString());
        }
    }

    private static void translateEquipToken(string token) {
        if (token.Contains("/imgdir")) {
            status -= 1;
        } else if (token.Contains("imgdir")) {
            if (status == 1) {
                if (!getName(token).Equals("info")) {
                    forwardCursor(status);
                }
            }

            status += 1;
        } else {
            if (status == 2) {
                string d = getName(token);

                if (d.Equals("cash")) {
                    if (!getValue(token).Equals("0")) {
                        nxItems.Add(currentItemid);
                    }

                    forwardCursor(status);
                }
            }
        }
    }

    private static void inspectItemWzEntry() {
        string line = null;

        try {
            while ((line = bufferedReader.readLine()) != null) {
                translateItemToken(line);
            }
        } catch (Exception e) {
            Log.Logger.Error(e.ToString());
        }
    }

    private static void translateItemToken(string token) {
        if (token.Contains("/imgdir")) {
            status -= 1;
        } else if (token.Contains("imgdir")) {
            if (status == 1) {
                currentItemid = int.Parse(getName(token));
            } else if (status == 2) {
                if (!getName(token).Equals("info")) {
                    forwardCursor(status);
                }
            }

            status += 1;
        } else {
            if (status == 3) {
                string d = getName(token);

                if (d.Equals("cash")) {
                    if (!getValue(token).Equals("0")) {
                        nxItems.Add(currentItemid);
                    }

                    forwardCursor(status);
                }
            }
        }
    }

    private static void printReportFileHeader() {
        printWriter.println(" # Report File autogenerated from the MapleCashDropFetcher feature by Ronan Lana.");
        printWriter.println(" # Generated data takes into account several data info from the underlying DB and the server-side WZ.xmls.");
        printWriter.println();
    }

    private static void listFiles(Path directoryName, <Path> files) {
        // get all the files from a directory
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(directoryName)) {
            foreach(Path path in stream) {
                if (Files.isRegularFile(path)) {
                    files.Add(path);
                } else if (Files.isDirectory(path)) {
                    listFiles(path, files);
                }
            }
        } catch (IOException e) {
            // TODO Auto-generated catch block
            Log.Logger.Error(e.ToString());
        }
    }

    private static int getItemIdFromFilename(string name) {
        try {
            return int.Parse(name.Substring(0, name.indexOf('.')));
        } catch (Exception e) {
            return -1;
        }
    }

    private static string getDropTableName(bool dropdata) {
        return (dropdata ? "drop_data" : "reactordrops");
    }

    private static string getDropElementName(bool dropdata) {
        return (dropdata ? "dropperid" : "reactorid");
    }

    private static void filterNxDropsOnDB(bool dropdata) {
        nxDrops.Clear();

        PreparedStatement ps = con.prepareStatement("SELECT DISTINCT itemid FROM " + getDropTableName(dropdata));
        ResultSet rs = ps.executeQuery();

        while (rs.next()) {
            int itemid = rs.getInt("itemid");

            if (nxItems.Contains(itemid)) {
                nxDrops.Add(itemid);
            }
        }

        rs.close();
        ps.close();
    }

    private static List<Pair<int, int>> getNxDropsEntries(bool dropdata) {
        List<Pair<int, int>> entries = new ();

        List<int> sortedNxDrops = new (nxDrops);
        Collections.sort(sortedNxDrops);

        foreach(int nx in sortedNxDrops) {
            PreparedStatement ps = con.prepareStatement("SELECT " + getDropElementName(dropdata) + " FROM " + getDropTableName(dropdata) + " WHERE itemid = ?");
            ps.setInt(1, nx);

            ResultSet rs = ps.executeQuery();
            while (rs.next()) {
                entries.Add(new (nx, rs.getInt(getDropElementName(dropdata))));
            }

            rs.close();
            ps.close();
        }

        return entries;
    }

    private static void reportNxDropResults(bool dropdata) {
        filterNxDropsOnDB(dropdata);

        if (nxDrops.Count > 0) {
            List<Pair<int, int>> nxEntries = getNxDropsEntries(dropdata);

            printWriter.println("NX DROPS ON " + getDropTableName(dropdata));
            foreach(Pair<int, int> nx in nxEntries) {
                printWriter.println(nx.left + " : " + nx.right);
            }
            printWriter.println("\n\n\n");
        }
    }

    private static void reportNxDropData() {
        try (con; PrintWriter pw = new PrintWriter(Files.newOutputStream(OUTPUT_FILE))) {
            Console.WriteLine("Reading Character.wz ...");
            <Path> files = new ();
            listFiles(WZFiles.CHARACTER.getFile(), files);

            foreach(Path path in files) {
                // Console.WriteLine("Parsing " + f.getAbsolutePath());
                int itemid = getItemIdFromFilename(path.getFileName().ToString());
                if (itemid < 0) {
                    continue;
                }

                bufferedReader = Files.newBufferedReader(path);

                currentItemid = itemid;
                inspectEquipWzEntry();

                bufferedReader.close();
            }

            Console.WriteLine("Reading Item.wz ...");
            files = new ();
            listFiles(WZFiles.ITEM.getFile(), files);

            foreach(Path path in files) {
                // Console.WriteLine("Parsing " + f.getAbsolutePath());
                bufferedReader = Files.newBufferedReader(path);

                if (path.getFileName().ToString().Length <= ITEM_FILE_NAME_SIZE) {
                    inspectItemWzEntry();
                } else { // pet file structure is similar to equips, maybe there are other item-types
                         // following this behaviour?
                    int itemid = getItemIdFromFilename(path.getFileName().ToString());
                    if (itemid < 0) {
                        continue;
                    }

                    currentItemid = itemid;
                    inspectEquipWzEntry();
                }

                bufferedReader.close();
            }

            Console.WriteLine("Reporting results...");

            // report suspects of missing quest drop data, as well as those drop data that
            // may have incorrect questids.
            printWriter = pw;
            printReportFileHeader();

            reportNxDropResults(true);
            reportNxDropResults(false);

            /*
             * printWriter.println("NX LIST"); // list of all cash items found foreach(int * nx in nxItems) { printWriter.println(nx); }
             */

            Console.WriteLine("Done!");
        } catch (SQLException e) {
            Console.WriteLine("Warning: Could not establish connection to database to report quest data.");
            Console.WriteLine(e.getMessage());
        } catch (Exception e) {
            Log.Logger.Error(e.ToString());
        }
    }

    public static void main(string[] args) {
        reportNxDropData();
    }
}
