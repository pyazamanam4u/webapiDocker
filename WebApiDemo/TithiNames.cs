namespace WebApiDemo
{
    public static class TithiNames
    {
        public static (string[] English, string[] Telugu) GetNames()
        {
            var eng = new[]
            {
                "Prathama","Dvitiya","Tritiya","Chaturthi","Panchami","Shashthi","Saptami","Ashtami","Navami","Dashami",
                "Ekadashi","Dwadashi","Trayodashi","Chaturdashi","Purnima/Amavasya","Prathama","Dvitiya","Tritiya","Chaturthi","Panchami",
                "Shashthi","Saptami","Ashtami","Navami","Dashami","Ekadashi","Dwadashi","Trayodashi","Chaturdashi","Purnima/Amavasya"
            };
            var tel = new[]
            {
                "ప్రథమ","ద్వితీయ"," తృతీయ","చతుర్థి","పంచమి","షష్ఠి","సప్తమి","అష్టమి","నవమి","దశమి",
                "ఎకాదశి","ద్వాదశి","త్రయోదశి","చతుర్దశి","పూర్ణిమ/అమావాస్య","ప్రథమ","ద్వితీయ"," తృతీయ","చతుర్థి","పంచమి",
                "షష్ఠి","సప్తమి","అష్టమి","నవమి","దశమి","ఎకాదశి","ద్వాదశి","త్రయోదశి","చతుర్దశి","పూర్ణిమ/అమావాస్య"
            };
            return (eng, tel);
        }
    }
}