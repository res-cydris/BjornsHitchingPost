using System;
using BjornsHitchingPost;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
float x=0,y=0,z=0,vx=1,vy=2,vz=3;
Check(!TetherMath.Constrain(5, ref x, ref y, ref z, ref vx, ref vy, ref vz), "At anchor should not constrain");
Check(vx==1 && vy==2 && vz==3, "Free velocity inside radius");
x=6; vx=2; vy=3; vz=4;
Check(TetherMath.Constrain(5, ref x, ref y, ref z, ref vx, ref vy, ref vz), "Outside should constrain");
Check(x==5 && vx==0 && vy==3 && vz==4, "Remove outward speed; preserve tangent");
x=6; vx=-2;
TetherMath.Constrain(5, ref x, ref y, ref z, ref vx, ref vy, ref vz);
Check(vx==-2, "Retain inward velocity");
x=5; vx=2;
Check(!TetherMath.Constrain(5, ref x, ref y, ref z, ref vx, ref vy, ref vz), "Exact boundary should not move");
var random=new Random(892970);
for(int i=0;i<10000;i++)
{
    x=(float)(random.NextDouble()*40-20); y=(float)(random.NextDouble()*40-20); z=(float)(random.NextDouble()*40-20);
    vx=(float)(random.NextDouble()*20-10); vy=(float)(random.NextDouble()*20-10); vz=(float)(random.NextDouble()*20-10);
    float before=vx*vx+vy*vy+vz*vz;
    bool changed=TetherMath.Constrain(5,ref x,ref y,ref z,ref vx,ref vy,ref vz);
    Check(x*x+y*y+z*z<=25.00002f, "Position outside tether");
    Check(vx*vx+vy*vy+vz*vz<=before+.0002f, "Constraint adds kinetic energy");
    if(changed) Check(x*vx+y*vy+z*vz<.0001f, "Outward velocity survives");
}
Console.WriteLine($"PASS: {checks} tether geometry assertions (10,000 deterministic randomized cases).");
int geometryChecks = checks;
Check(OwnershipPolicy.Select(1, 2, false, new long[]{2,3}) == 2, "Retain current modded simulator");
Check(OwnershipPolicy.Select(1, 99, false, new long[]{2,3}) == 2, "Never retain vanilla simulator");
Check(OwnershipPolicy.Select(1, 2, false, Array.Empty<long>()) == 1, "Park on server after last modded client leaves");
Check(OwnershipPolicy.Select(1, 1, false, new long[]{3}) == 3, "Resume after modded player returns");
Check(OwnershipPolicy.Select(1, 2, true, Array.Empty<long>()) == 1, "Host simulates nearby");
Check(OwnershipPolicy.Select(1, 2, true, new long[]{2}) == 2, "Keep live harpoon's modded holder");
for (int i=0;i<10000;i++)
{
    long preferred=random.Next(1,10);
    bool active=random.Next(2)==0;
    var nearby = new System.Collections.Generic.List<long>();
    for(long peer=2;peer<=9;peer++) if(random.Next(2)==0) nearby.Add(peer);
    long result=OwnershipPolicy.Select(1,preferred,active,nearby);
    Check(result==1 || nearby.Contains(result), "Owner must be server or compatible nearby peer");
    if(nearby.Contains(preferred)) Check(result==preferred, "Avoid unnecessary ownership handoff");
    if(!active && nearby.Count>0) Check(result!=1, "Resume rather than park when simulator available");
}
Console.WriteLine($"PASS: {checks-geometryChecks} ownership assertions (10,000 deterministic randomized scenarios).");
int configuredChecks = checks;
foreach (float radius in new[] { 1f, 2.5f, 5f, 15f, 30f })
{
    x = radius + 2f; y = z = 0f; vx = 3f; vy = 1f; vz = 0f;
    Check(TetherMath.Constrain(radius, ref x, ref y, ref z, ref vx, ref vy, ref vz), "Configured radius must constrain");
    Check(Math.Abs(x - radius) < .0001f && Math.Abs(vx) < .0001f && vy == 1f, "Configured boundary and tangential velocity");
    x = radius * .5f; vx = -2f;
    Check(!TetherMath.Constrain(radius, ref x, ref y, ref z, ref vx, ref vy, ref vz), "Inside configured distance stays free");
}
x = 20f; y = z = 0f; vx = vy = vz = 0f;
TetherMath.Constrain(30f, ref x, ref y, ref z, ref vx, ref vy, ref vz);
Check(x == 20f, "Long tether retains position");
TetherMath.Constrain(2.5f, ref x, ref y, ref z, ref vx, ref vy, ref vz);
Check(x == 2.5f, "Shortening existing tether brings animal inside new limit");
Console.WriteLine($"PASS: {checks-configuredChecks} configurable-distance assertions.");
