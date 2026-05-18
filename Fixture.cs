using System.Collections;
using System.Collections.Generic;

namespace MDMX_MaskCreator;

public class Fixture
{
    string name = "Luma Glow";
    int channel_count = 12;
    string notes = "Luma Glow avatar effects";
    List<Channel> channels = null;
}

public class Channel
{
    int id = 1;
    string name = "Red";
    string type = "Linear";
    string notes = "";
    string special = "Channel 1";
    string status = "Working";
    
    
    
}