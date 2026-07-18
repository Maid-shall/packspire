using System.Collections.Generic;
using UnityEngine;

namespace Packspire {
/// <summary>Shared green/black chroma key with edge spill reduction for route and hub art.</summary>
public static class PackspireChromaKey {
 static readonly Dictionary<int,Texture2D> Cache=new();

 /// <summary>Readable copy for Sprite.Create without chroma (cached).</summary>
 public static Texture2D Readable(Texture2D source,List<Texture2D> sink=null){
  if(source==null)return null;
  if(source.isReadable)return source;
  int key=source.GetInstanceID()^4;
  if(Cache.TryGetValue(key,out var cached)&&cached!=null)return cached;
  var readable=EnsureReadable(source);
  if(readable==null){
   Debug.LogWarning($"[PackspireChromaKey] Failed to read texture '{source.name}' — hiding layer.");
   return null;
  }
  Cache[key]=readable;
  sink?.Add(readable);
  return readable;
 }

 public static Texture2D Key(Texture2D source,bool keyGreen,bool keyBlack,List<Texture2D> sink=null){
  if(source==null)return null;
  if(!keyGreen&&!keyBlack)return Readable(source,sink);
  int key=source.GetInstanceID()^(keyGreen?1:0)^(keyBlack?2:0);
  if(Cache.TryGetValue(key,out var cached)&&cached!=null)return cached;
  var readable=EnsureReadable(source);
  if(readable==null){
   Debug.LogWarning($"[PackspireChromaKey] Failed to read texture '{source.name}' — hiding layer.");
   return null;
  }
  var tex=new Texture2D(readable.width,readable.height,TextureFormat.RGBA32,false){name=source.name+"_keyed"};
  var pixels=readable.GetPixels();
  int opaque=0,greenKilled=0;
  for(int i=0;i<pixels.Length;i++){
   var c=pixels[i];
   if(keyBlack&&c.r<.07f&&c.g<.07f&&c.b<.07f){c=Color.clear;pixels[i]=c;continue;}
   if(keyGreen){
    float competing=Mathf.Max(c.r,c.b);
    float dominance=c.g-competing;
    float strength=Mathf.InverseLerp(.012f,.085f,dominance)*Mathf.InverseLerp(.18f,.48f,c.g);
    // Magenta-ish spill cleanup on edges.
    float spill=Mathf.Clamp01((c.g-competing)*.9f);
    if(strength>0f||spill>0.04f){
     float kill=Mathf.Max(strength,spill*.85f);
     c.a*=1f-Mathf.Clamp01(kill);
     c.g=Mathf.Min(c.g,competing*1.04f+.02f);
     if(kill>.92f){c.r=c.g=c.b=0f;c.a=0f;greenKilled++;}
    }
   }
   if(c.a>.08f)opaque++;
   pixels[i]=c;
  }
  float opaqueRatio=opaque/(float)Mathf.Max(1,pixels.Length);
  if(keyGreen&&opaqueRatio<.02f){
   Debug.LogWarning($"[PackspireChromaKey] Keying wiped '{source.name}' — hiding layer.");
   if(readable!=source)Object.Destroy(readable);
   Object.Destroy(tex);
   return null;
  }
  // Residual green check on opaque pixels.
  int residualGreen=0,opaqueCheck=0;
  for(int i=0;i<pixels.Length;i++){
   var c=pixels[i];
   if(c.a<.2f)continue;
   opaqueCheck++;
   if(c.g>c.r+.12f&&c.g>c.b+.12f&&c.g>.35f)residualGreen++;
  }
  if(keyGreen&&opaqueCheck>0&&residualGreen/(float)opaqueCheck>.35f){
   Debug.LogWarning($"[PackspireChromaKey] Residual chroma green on '{source.name}' — hiding layer.");
   if(readable!=source)Object.Destroy(readable);
   Object.Destroy(tex);
   return null;
  }
  tex.SetPixels(pixels);
  // Keep readable so callers can crop transparent margins for cover scaling.
  tex.Apply(false,false);
  if(readable!=source)Object.Destroy(readable);
  var cropped=CropToOpaque(tex,.05f);
  if(cropped!=null&&cropped!=tex){
   Object.Destroy(tex);
   tex=cropped;
  } else if(tex.isReadable)tex.Apply(false,true);
  Cache[key]=tex;
  sink?.Add(tex);
  return tex;
}

 /// <summary>
 /// Trim fully-transparent margins so cover scale uses the painted content,
 /// not empty padding left after chroma key.
 /// </summary>
 public static Texture2D CropToOpaque(Texture2D source,float alphaThreshold=.05f){
  if(source==null)return null;
  var readable=source.isReadable?source:EnsureReadable(source);
  if(readable==null)return source;
  int w=readable.width,h=readable.height;
  var pixels=readable.GetPixels();
  int minX=w,minY=h,maxX=-1,maxY=-1;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   if(pixels[y*w+x].a<=alphaThreshold)continue;
   if(x<minX)minX=x;if(y<minY)minY=y;
   if(x>maxX)maxX=x;if(y>maxY)maxY=y;
  }
  if(maxX<minX||maxY<minY){
   if(readable!=source)Object.Destroy(readable);
   return source;
  }
  // Pad 1px so bilinear edges do not clip.
  minX=Mathf.Max(0,minX-1);minY=Mathf.Max(0,minY-1);
  maxX=Mathf.Min(w-1,maxX+1);maxY=Mathf.Min(h-1,maxY+1);
  int cw=maxX-minX+1,ch=maxY-minY+1;
  if(cw>=w-2&&ch>=h-2){
   if(readable!=source)Object.Destroy(readable);
   return source;
  }
  var cropped=new Texture2D(cw,ch,TextureFormat.RGBA32,false){name=source.name+"_crop"};
  var outPx=new Color[cw*ch];
  for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)
   outPx[y*cw+x]=pixels[(minY+y)*w+(minX+x)];
  cropped.SetPixels(outPx);
  cropped.Apply(false,true);
  if(readable!=source)Object.Destroy(readable);
  return cropped;
}

 static Texture2D EnsureReadable(Texture2D source){
  if(source==null)return null;
  if(source.isReadable)return source;
  var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
  var prev=RenderTexture.active;
  try{
   Graphics.Blit(source,rt);
   RenderTexture.active=rt;
   var tex=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
   tex.ReadPixels(new Rect(0,0,source.width,source.height),0,0);
   tex.Apply();
   return tex;
  } finally {
   RenderTexture.active=prev;
   RenderTexture.ReleaseTemporary(rt);
  }
 }
}
}
