using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace MDMX_MaskCreator;

/// <summary>
/// Fixture header
/// </summary>
public class DmxFixture
{
    public string Name { get; set; }        
    public int Channels { get; set; }      
    public string Notes { get; set; }       
    public List<DmxChannel> ChannelList { get; set; } = new();
    
    public static List<DmxFixture> ParseFixtures(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Read();
        csv.ReadHeader();
        
        var fixtures = new List<DmxFixture>();
        DmxFixture current = null;
        while (csv.Read())
        {
            var status = csv.GetField("Status");
            var name = csv.GetField("Channel Name");
            var channel = csv.GetField("Channel");
            var type = csv.GetField("Type");
            var notes = csv.GetField("Notes");
            var special = csv.GetField("Special");
            
            // skip empty seps
            if (string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(name))
                continue;
            
            // start new fixture
            if (status == "HEAD")
            {

                current = new DmxFixture
                {
                    Name = name,
                    Channels = int.Parse(channel.Replace("ch", "")),
                    Notes = notes,
                }; 
                fixtures.Add(current);
            } else if (current != null && int.TryParse(channel, out int chNum))
            {
                current.ChannelList.Add(new DmxChannel
                {
                    Status = status,
                    Special = special,
                    ChannelName = name,
                    Channel = chNum,
                    Type = type,
                    Notes = notes
                });
            }
        }
        return fixtures;
    }
    
}
/// <summary>
/// Actual channels in dmx (not important for this lol)
/// </summary>
public class DmxChannel
{
    public string Status { get; set; }      
    public string Special { get; set; }     
    public string ChannelName { get; set; }
    public int Channel { get; set; }        
    public string Type { get; set; }       
    public string Notes { get; set; }
}
