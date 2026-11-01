using Lemegeton.Core;

var tests = new (string Name, Action Run)[]
{
    ("six distinct recipients, stable order and no automatic clearing", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1199); s.Expect(0);
        s.Tick(1200); s.Expect(2); Check(s.Marks[1] == 7 && s.Marks[2] == 8);
        s.Tick(11199); s.Expect(2); s.Tick(11200); s.Expect(2); s.Tick(999999); s.Expect(2);
    }),
    ("party and status event permutations", () => {
        for (int seed = 0; seed < 100; seed++) {
            var s = new Simulation(); var r = new Random(seed);
            s.Party = s.Party.OrderBy(_ => r.Next()).ToArray(); s.Begin();
            foreach (uint actor in Enumerable.Range(1, 6).OrderBy(_ => r.Next())) s.Status(actor);
            s.Tick(1200); Check(s.Marks[1] == 7 && s.Marks[2] == 8);
        }
    }),
    ("duplicates do not count as distinct recipients", () => {
        var s = new Simulation(); s.Begin(); for (int i=0;i<10;i++) s.Status(1);
        s.Tick(1200); s.Expect(0); s.FeedSix(); s.Tick(1200); s.Expect(2);
    }),
    ("duplicate casts do not reset collection or reassign", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Begin(1100); s.Tick(1200); s.Expect(2);
        s.Begin(9000); s.Tick(11200); s.Expect(2); Check(s.Assignments == 2);
    }),
    ("incomplete and expired events never assign", () => {
        var s = new Simulation(); s.Begin(); for(uint i=1;i<6;i++) s.Status(i);
        s.Tick(7000); s.Status(6, 8000); s.Tick(8200); s.Expect(0);
    }),
    ("status before cast is ignored", () => {
        var s = new Simulation(); s.FeedSix(); s.Begin(); s.Tick(1200); s.Expect(0);
    }),
    ("unrelated cast, status and nonparty actor ignored", () => {
        var s = new Simulation(); s.Tracker.Begin(123, s.Party, 1000, 1); s.FeedSix(); s.Tick(1200); s.Expect(0);
        s.Begin(); s.Tracker.Status(7, 123, true, 1000); s.Status(99); s.FeedSix(); s.Tick(1200); s.Expect(2);
    }),
    ("live statuses must match packet recipients", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Live.Remove(6); s.Tick(1200); s.Expect(0);
        s.Live[6] = Simulation.Element(6); s.Tick(1500); s.Expect(2);
    }),
    ("seventh recipient cancels rather than guessing", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Status(7); s.Tick(1200); s.Expect(0);
    }),
    ("seventh live status prevents assignment", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Live[7]=0xB56; s.Tick(1200); s.Expect(0);
    }),
    ("delayed seventh recipient leaves assigned markers for manual clearing", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1200); s.Expect(2);
        s.Status(7, 1400); s.Expect(2); s.Tick(11200); Check(s.Assignments==2);
    }),
    ("late live status does not remove assigned markers", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1200); s.Expect(2);
        s.Live[7]=0xB56; s.Tick(1400); s.Expect(2);
    }),
    ("status loss and conflicting element cancel", () => {
        foreach(bool loss in new[]{true,false}) {
            var s = new Simulation(); s.Begin(); s.FeedSix();
            s.Tracker.Status(1, loss ? Simulation.Element(1) : 0xB57, !loss, 1100);
            s.Tick(1400); s.Expect(0);
        }
    }),
    ("wipe reset retains markers and rejects late packets", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1200); s.Tracker.Reset(); s.Expect(2);
        s.FeedSix(); s.Begin(2000); s.Tick(2200); s.Expect(2); Check(s.Assignments==2);
    }),
    ("party change cancels pending detection without clearing markers", () => {
        foreach(bool assigned in new[]{true,false}) {
            var s = new Simulation(); s.Begin(); s.FeedSix(); if(assigned) s.Tick(1200);
            s.Party[7]=9; s.Tick(1300); s.Expect(assigned ? 2 : 0); s.Status(6,1400); s.Tick(1600); s.Expect(assigned ? 2 : 0);
        }
    }),
    ("disable blocks pending assignment without clearing existing markers", () => {
        foreach(bool assigned in new[]{true,false}) {
            var s = new Simulation(); s.Begin(); s.FeedSix(); if(assigned) s.Tick(1200);
            s.Tracker.Tick(s.Party,s.Live,1300,false); s.Expect(assigned ? 2 : 0);
        }
    }),
    ("invalid party sizes, duplicate and invalid actors rejected", () => {
        foreach(var party in new[]{new uint[]{1,2,3},new uint[]{1,2,3,4,5,6,7,7},new uint[]{0,2,3,4,5,6,7,8},new uint[]{0xE0000000,2,3,4,5,6,7,8}}) {
            var s = new Simulation {Party=party}; s.Begin(); s.FeedSix(); s.Tick(1200); s.Expect(0);
        }
    }),
    ("manual macro clear allows a subsequent cast to assign again", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1200); s.Tick(11200); s.Expect(2); s.Marks.Clear();
        s.Begin(12000); s.FeedSix(12000); s.Tick(12200); s.Expect(2); s.Tick(22200); s.Expect(2);
        Check(s.Assignments == 4);
    }),
    ("partial assignment remains for manual clearing", () => {
        var s = new Simulation {FailSecond=true}; s.Begin(); s.FeedSix(); s.Tick(1200); s.Expect(1); Check(s.Marks[1]==7);
    }),
    ("elapsed time preserves moved and unrelated markers", () => {
        var s = new Simulation(); s.Begin(); s.FeedSix(); s.Tick(1200); s.Marks[1]=99; s.Marks[3]=4;
        s.Tick(11200); Check(s.Marks.Count==3 && s.Marks[1]==99 && s.Marks[2]==8 && s.Marks[3]==4);
    }),
    ("preexisting sign is not overwritten", () => {
        var s = new Simulation(); s.Marks[1]=99; s.Begin(); s.FeedSix(); s.Tick(1200);
        Check(s.Assignments==0 && s.Marks[1]==99);
    }),
    ("preexisting target marker is not overwritten", () => {
        var s = new Simulation(); s.Marks[3]=7; s.Begin(); s.FeedSix(); s.Tick(1200);
        Check(s.Assignments==0 && s.Marks[3]==7);
    }),
    ("shared assignment rules handle empty slots and occupied targets", () => {
        Check(ScopedMarkerRules.CanAssign(7,0,new ulong[]{0,0xE0000000}));
        Check(ScopedMarkerRules.CanAssign(7,0xE0000000,Array.Empty<ulong>()));
        Check(!ScopedMarkerRules.CanAssign(7,99,Array.Empty<ulong>()));
        Check(!ScopedMarkerRules.CanAssign(7,0,new ulong[]{7}));
        Check(!ScopedMarkerRules.CanAssign(0,0,Array.Empty<ulong>()));
    }),
};
int failed=0;
foreach(var test in tests) {
    try {test.Run(); Console.WriteLine("PASS " + test.Name);}
    catch(Exception ex) {failed++; Console.WriteLine("FAIL " + test.Name + ": " + ex.Message);}
}
Console.WriteLine($"{tests.Length-failed}/{tests.Length} simulations passed (including 100 shuffled event/party sequences).");
return failed == 0 ? 0 : 1;

static void Check(bool ok) {if(!ok) throw new Exception("Assertion failed");}

sealed class Simulation
{
    internal uint[] Party=Enumerable.Range(1,8).Select(x=>(uint)x).ToArray();
    internal readonly Dictionary<uint,uint> Live=new();
    internal readonly Dictionary<int,uint> Marks=new();
    internal readonly CelestriadTracker Tracker=new();
    internal int Assignments;
    internal bool FailSecond;
    internal Simulation() {
        Tracker.Apply=(actor,sign)=> {
            Marks.TryGetValue(sign, out uint holder);
            if((FailSecond && sign==2) || !ScopedMarkerRules.CanAssign(actor,holder,Marks.Values.Select(x=>(ulong)x))) return false;
            Marks[sign]=actor; Assignments++; return true;
        };
    }
    internal static uint Element(uint actor)=>(actor%3) switch {0=>0xB57u,1=>0xB56u,_=>0xBB6u};
    internal void Begin(long now=1000)=>Tracker.Begin(CelestriadTracker.Cast,Party,now,1);
    internal void Status(uint actor,long now=1000) {if(Party.Contains(actor)) Live[actor]=Element(actor);Tracker.Status(actor,Element(actor),true,now);}
    internal void FeedSix(long now=1000) {for(uint i=1;i<=6;i++) Status(i,now);}
    internal void Tick(long now)=>Tracker.Tick(Party,Live,now,true);
    internal void Expect(int count) {if(Marks.Count!=count) throw new Exception($"Expected {count} markers, got {Marks.Count}");}
}
