using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
namespace TvAnti {
 public sealed class TvAntiSoScanner {
  readonly TvAntiConfig c; public TvAntiSoScanner(TvAntiConfig c){this.c=c;}
  public IEnumerable<Violation> Scan(){
   if(!Application.platform.ToString().Contains("Android")||!c.scanSo)yield break;
   var dirs=new[]{Application.persistentDataPath,Application.temporaryCachePath,Application.dataPath,Path.GetDirectoryName(Application.dataPath)};
   var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var dir in dirs.Where(x=>!string.IsNullOrEmpty(x)&&Directory.Exists(x))){
    IEnumerable<string> files;
    try{files=Directory.EnumerateFiles(dir,"*.so",SearchOption.AllDirectories);}catch{continue;}
    foreach(var f in files){
     if(!seen.Add(f))continue;
     string n=Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
     if(c.blockedSoNames.Any(x=>string.Equals(n,x,StringComparison.OrdinalIgnoreCase)))
      yield return new Violation(ViolationType.BlockedLibrary,1,$"blocked basename: {n}");
     if(c.blockedSoSha256.Count>0){string h=Hash(f);if(c.blockedSoSha256.Any(x=>string.Equals(x,h,StringComparison.OrdinalIgnoreCase)))yield return new Violation(ViolationType.LibraryHashMatch,1,"blocked SHA-256 match");}
    }
   }
  }
  static string Hash(string p){using(var sha=SHA256.Create())using(var s=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 }
}
