/* NIVEL CERO / ZERO PROTOCOL | Single-file, dependency-free WebGL2 game.
   Original playable 3D vertical slice with mouse-look FPS/TPS cameras. No external networks/assets required. */
(() => {
'use strict';
const $=id=>document.getElementById(id), canvas=$('world');
const gl=canvas.getContext('webgl2',{antialias:true,alpha:false,powerPreference:'high-performance'});
if(!gl){$('menu').innerHTML='<div class="simple-card"><h2>WebGL 2 no disponible</h2><p>Probá con Chrome o Edge actualizado con aceleración gráfica activada.</p></div>';return}
const C={ink:[.028,.059,.105],deep:[.025,.078,.144],plate:[.075,.155,.253],plate2:[.10,.22,.34],cyan:[.22,.88,.95],aqua:[.6,1,.95],pink:[1,.22,.43],violet:[.58,.33,.98],yellow:[1,.79,.27],green:[.37,1,.67],white:[.87,.98,1],fog:[.019,.048,.094]};
const clamp=(v,a,b)=>Math.min(b,Math.max(a,v)),lerp=(a,b,t)=>a+(b-a)*t,dist2=(x,z,X,Z)=>Math.hypot(x-X,z-Z);
const VS=`#version 300 es
precision highp float;
in vec3 aPos;in vec3 aNormal;in vec2 aUV;
uniform mat4 uMVP;uniform mat4 uModel;
out vec3 vNorm;out vec3 vPos;out vec2 vUV;
void main(){vec4 p=uModel*vec4(aPos,1.);vPos=p.xyz;vNorm=mat3(uModel)*aNormal;vUV=aUV;gl_Position=uMVP*vec4(aPos,1.);}`;
const FS=`#version 300 es
precision highp float;
in vec3 vNorm;in vec3 vPos;in vec2 vUV;
uniform vec3 uColor;uniform vec3 uCam;uniform float uEmissive;uniform float uGrid;uniform float uTime;uniform float uAlpha;
out vec4 outColor;
void main(){
 vec3 n=normalize(vNorm);vec3 l=normalize(vec3(-.45,.83,.35));
 float diffuse=max(dot(n,l),0.);
 float amb=.36+diffuse*.54;
 vec3 col=uColor*amb;
 float rim=pow(1.-max(dot(n,normalize(uCam-vPos)),0.),2.4);
 col += uColor*rim*.18;
 if(uGrid>0.5 && n.y>.5){
   vec2 g=abs(fract(vPos.xz*.66)-.5);
   float lines=smoothstep(.46,.49,max(g.x,g.y));
   col+=uColor*lines*.11;
   float scan=sin(vPos.z*.7-uTime*1.8)*.5+.5;
   col+=uColor*scan*.035;
 }
 col+=uColor*uEmissive;
 float distanceToCamera=length(vPos-uCam);
 float fog=1.-exp(-distanceToCamera*.008);
 col=mix(col,vec3(.019,.051,.092),clamp(fog,0.,.78));
 outColor=vec4(col,uAlpha);
}`;
function shader(type,src){let s=gl.createShader(type);gl.shaderSource(s,src);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error(gl.getShaderInfoLog(s));return s}
let prog=gl.createProgram();gl.attachShader(prog,shader(gl.VERTEX_SHADER,VS));gl.attachShader(prog,shader(gl.FRAGMENT_SHADER,FS));gl.linkProgram(prog);
if(!gl.getProgramParameter(prog,gl.LINK_STATUS))throw Error(gl.getProgramInfoLog(prog));gl.useProgram(prog);
const U={};for(const name of ['uMVP','uModel','uColor','uCam','uEmissive','uGrid','uTime','uAlpha'])U[name]=gl.getUniformLocation(prog,name);
const layout=(data)=>{const vao=gl.createVertexArray();gl.bindVertexArray(vao);let vbo=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,vbo);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array(data),gl.STATIC_DRAW);for(let i=0;i<3;i++){gl.enableVertexAttribArray(i);gl.vertexAttribPointer(i,[3,3,2][i],gl.FLOAT,false,32,[0,12,24][i])}gl.bindVertexArray(null);return{vao,count:data.length/8}}
function cube(){let out=[];let f=[
 [[-1,0,0],[-.5,-.5,-.5],[-.5,-.5,.5],[-.5,.5,.5],[-.5,.5,-.5]],
 [[1,0,0],[.5,-.5,.5],[.5,-.5,-.5],[.5,.5,-.5],[.5,.5,.5]],
 [[0,-1,0],[-.5,-.5,.5],[-.5,-.5,-.5],[.5,-.5,-.5],[.5,-.5,.5]],
 [[0,1,0],[-.5,.5,-.5],[-.5,.5,.5],[.5,.5,.5],[.5,.5,-.5]],
 [[0,0,-1],[.5,-.5,-.5],[-.5,-.5,-.5],[-.5,.5,-.5],[.5,.5,-.5]],
 [[0,0,1],[-.5,-.5,.5],[.5,-.5,.5],[.5,.5,.5],[-.5,.5,.5]]
 ];for(const [n,a,b,c,d] of f){const verts=[a,b,c,a,c,d];verts.forEach((p,i)=>{out.push(...p,...n,...[[0,0],[1,0],[1,1],[0,0],[1,1],[0,1]][i])})}return layout(out)}
function sphere(N=13,M=9){let arr=[];const vertex=(a,b)=>{let th=a*Math.PI*2,ph=(b-.5)*Math.PI,x=Math.cos(ph)*Math.cos(th),y=Math.sin(ph),z=Math.cos(ph)*Math.sin(th);arr.push(x*.5,y*.5,z*.5,x,y,z,a,b)};for(let i=0;i<N;i++)for(let j=0;j<M;j++){let a=i/N,b=j/M,c=(i+1)/N,d=(j+1)/M;vertex(a,b);vertex(c,b);vertex(c,d);vertex(a,b);vertex(c,d);vertex(a,d)}return layout(arr)}
function torus(R=.43,r=.07,major=30,minor=8){const out=[];const vert=(u,v)=>{let a=u*2*Math.PI,b=v*2*Math.PI,co=Math.cos(a),si=Math.sin(a),c=Math.cos(b),s=Math.sin(b);out.push((R+r*c)*co,(R+r*c)*si,r*s,co*c,si*c,s,u,v)};for(let i=0;i<major;i++)for(let j=0;j<minor;j++){let a=i/major,b=j/minor,c=(i+1)/major,e=(j+1)/minor;vert(a,b);vert(c,b);vert(c,e);vert(a,b);vert(c,e);vert(a,e)}return layout(out)}
function octa(){const o=[],pts=[[0,.5,0],[.45,0,0],[0,0,.45],[-.45,0,0],[0,0,-.45],[0,-.5,0]];const tris=[[0,1,2],[0,2,3],[0,3,4],[0,4,1],[5,2,1],[5,3,2],[5,4,3],[5,1,4]];for(let t of tris){let a=pts[t[0]],b=pts[t[1]],c=pts[t[2]],u=[b[0]-a[0],b[1]-a[1],b[2]-a[2]],v=[c[0]-a[0],c[1]-a[1],c[2]-a[2]],n=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]],ll=Math.hypot(...n);n=n.map(q=>q/ll);for(const p of [a,b,c])o.push(...p,...n,0,0)}return layout(o)}
const GEO={cube:cube(),sphere:sphere(),torus:torus(),octa:octa()};
function mul(A,B){let r=new Float32Array(16);for(let c=0;c<4;c++)for(let i=0;i<4;i++){r[c*4+i]=A[i]*B[c*4]+A[4+i]*B[c*4+1]+A[8+i]*B[c*4+2]+A[12+i]*B[c*4+3]}return r}
function pers(fov,asp,n,far){let f=1/Math.tan(fov/2),r=new Float32Array(16);r[0]=f/asp;r[5]=f;r[10]=(far+n)/(n-far);r[11]=-1;r[14]=2*far*n/(n-far);return r}
function look(eye,tar){let z=[eye[0]-tar[0],eye[1]-tar[1],eye[2]-tar[2]],dl=Math.hypot(...z);z=z.map(v=>v/dl);let x=[z[2],0,-z[0]],xl=Math.hypot(...x);x=x.map(v=>v/xl);let y=[z[1]*x[2]-z[2]*x[1],z[2]*x[0]-z[0]*x[2],z[0]*x[1]-z[1]*x[0]];return new Float32Array([x[0],y[0],z[0],0,x[1],y[1],z[1],0,x[2],y[2],z[2],0,-x[0]*eye[0]-x[1]*eye[1]-x[2]*eye[2],-y[0]*eye[0]-y[1]*eye[1]-y[2]*eye[2],-z[0]*eye[0]-z[1]*eye[1]-z[2]*eye[2],1])}
function model(x,y,z,sx,sy,sz,ry=0,rz=0){let cy=Math.cos(ry),si=Math.sin(ry),cz=Math.cos(rz),sn=Math.sin(rz);return new Float32Array([cy*cz*sx,sn*sx,-si*cz*sx,0,-cy*sn*sy,cz*sy,si*sn*sy,0,si*sz,0,cy*sz,0,x,y,z,1])}
let cam=[12,12,-20],cameraTarget=[0,1,8], viewProj=new Float32Array(16),frameT=0,drawCount=0;
function draw(kind,x,y,z,sx,sy,sz,color,emit=0,grid=0,alpha=1,ry=0,rz=0){if(z<player.z-100||z>player.z+220)return;const mesh=GEO[kind];if(!mesh)return;const M=model(x,y,z,sx,sy,sz,ry,rz);gl.uniformMatrix4fv(U.uMVP,false,mul(viewProj,M));gl.uniformMatrix4fv(U.uModel,false,M);gl.uniform3fv(U.uColor,color);gl.uniform1f(U.uEmissive,emit);gl.uniform1f(U.uGrid,grid);gl.uniform1f(U.uAlpha,alpha);gl.bindVertexArray(mesh.vao);if(alpha<.999){gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA);gl.depthMask(false)}gl.drawArrays(gl.TRIANGLES,0,mesh.count);if(alpha<.999){gl.depthMask(true);gl.disable(gl.BLEND)}drawCount++}
function box(x,y,z,w,h,l,c,em=0,grid=0,a=1,ry=0){draw('cube',x,y,z,w,h,l,c,em,grid,a,ry)}
const platforms=[
{x:0,z:3,w:11,l:18,y:0,section:0},
{x:0,z:21,w:9,l:14,y:.4,section:0},
{x:3,z:35,w:9,l:13,y:.65,section:0},
{x:-6.5,z:34,w:5,l:11,y:.85,section:0,optional:true},
{x:0,z:49,w:11,l:15,y:.05,section:1},
{x:0,z:65,w:9,l:13,y:.4,section:1},
{x:-3,z:80,w:9,l:14,y:.3,section:1},
{x:0,z:96,w:12,l:17,y:0,section:2},
{x:0,z:120,w:24,l:31,y:0,section:2,arena:true},
{x:0,z:144,w:9,l:18,y:.15,section:2}];
const crystals=[{x:0,z:22,y:.4},{x:-6.6,z:34,y:.85},{x:-3,z:80,y:.3}];
const checkpoints=[{x:0,z:2,y:0},{x:1,z:50,y:.05},{x:0,z:97,y:0}];
const terminals=[{x:6,z:49,y:.05,type:'hazard',name:'FIREWALL SECTOR 02',off:0},{x:-6,z:117,y:0,type:'boss',name:'NODO ALFA',off:0},{x:6,z:117,y:0,type:'boss',name:'NODO BETA',off:0}];
const hazards=[{x:3,z:35,w:6,section:0},{x:0,z:66,w:7,section:1},{x:0,z:98,w:7,section:2}];
const drones=[{x:0,z:37,bx:0,bz:37,amp:1.7,rate:.7,phase:0,alive:true,cd:1},{x:0,z:66,bx:0,bz:66,amp:2.2,rate:.4,phase:2,alive:true,cd:1.6},{x:-3,z:84,bx:-3,bz:84,amp:2,rate:.6,phase:4,alive:true,cd:2.1},{x:0,z:103,bx:0,bz:103,amp:3.2,rate:.6,phase:6,alive:true,cd:1.7}];
const stars=Array.from({length:240},(_,i)=>{let x=Math.sin(i*99.31)*210,y=35+(i*43)%110,z=i*2.9-135;return{x,y,z,sz:.06+((i*9)%5)*.03}});
const bgStructures=Array.from({length:92},(_,i)=>{let z=-30+i*2.65,x=(i%2===0?-1:1)*(24+((i*7)%16)),h=8+((i*17)%30),w=3+(i%4);return{x,y:-8+h/2,z,h,w,c:(i%5?C.deep:C.plate)}});
const player={x:0,y:0,z:-2,vx:0,vy:0,vz:0,facingYaw:0,onGround:true,coyote:0,health:3,fragments:0,dashCd:0,dashT:0,dashDir:[0,1],invuln:0,checkpoint:0,fallY:-12,trail:[],steps:0,jumpPressed:false};
const boss={hp:3,active:false,defeated:false,shield:true,terminal:[false,false],vulnerable:0,hitCd:0,shotCd:2.7,spin:0};
const state={playing:false,paused:false,finished:false,elapsed:0,deaths:0,enemyKills:0,region:0,particles:[],projectiles:[],toastT:0,subtitleT:0,audio:null,mute:false,impact:0,shake:0,lastInteract:-1};
// WASD always moves relative to the view in BOTH camera modes.
// World forward is +Z at yaw=0. Camera's screen-right is world -X.
const keys=new Set();
let screenW=0,screenH=0,last=performance.now(),fpsSmooth=60;
let cameraMode='third',camYaw=0,camPitch=.08,camZoom=10.5,drag=false,dragXY=[0,0];
const LOOK_SPEED=.0025;
function directionFromKeys(){
 const right=Number(keys.has('KeyD')||keys.has('ArrowRight'))-Number(keys.has('KeyA')||keys.has('ArrowLeft'));
 const forward=Number(keys.has('KeyW')||keys.has('ArrowUp'))-Number(keys.has('KeyS')||keys.has('ArrowDown'));
 const n=Math.hypot(right,forward);
 if(!n)return {x:0,z:0,active:false};
 // Right = (-cos(yaw), -sin(yaw)); Forward = (-sin(yaw), cos(yaw)).
 return {x:(-right*Math.cos(camYaw)-forward*Math.sin(camYaw))/n,
         z:(forward*Math.cos(camYaw)-right*Math.sin(camYaw))/n,active:true};
}
function changeCamera(mode){
 cameraMode=mode==='first'?'first':'third';
 player.facingYaw=-camYaw;
 $('cameraModeLabel').textContent=cameraMode==='first'?'1ª PERSONA':'3ª PERSONA';
 $('cameraToggle').textContent=cameraMode==='first'?'CAMBIAR A 3ª PERSONA [V]':'CAMBIAR A 1ª PERSONA [V]';
 // Instantly move on switches so a third-person camera never clips through Zero's head.
 if(cameraMode==='first'){
   cam=[player.x,player.y+1.72,player.z];
   cameraTarget=[cam[0]-Math.sin(camYaw)*Math.cos(camPitch)*24,cam[1]+Math.sin(camPitch)*24,cam[2]+Math.cos(camYaw)*Math.cos(camPitch)*24];
 }else{
   cam=[player.x+Math.sin(camYaw)*camZoom,player.y+Math.max(1.25,3-Math.sin(camPitch)*camZoom*.53),player.z-Math.cos(camYaw)*camZoom];
   cameraTarget=[player.x-Math.sin(camYaw)*4.2,player.y+1.55+Math.sin(camPitch)*5.5,player.z+Math.cos(camYaw)*4.2];
 }
}
function applyMouse(dx,dy){
 camYaw=(camYaw+dx*LOOK_SPEED+Math.PI*2)%(Math.PI*2);
 camPitch=clamp(camPitch-dy*LOOK_SPEED,-.85,.75);
 player.facingYaw=-camYaw;
}
function requestMouseCapture(){
 if(document.pointerLockElement===canvas)return;
 try{const v=canvas.requestPointerLock?.();if(v?.catch)v.catch(()=>toast('Mantené clic izquierdo y arrastrá para mirar.',3))}catch(e){}
}
function setPaused(paused){
 state.paused=paused;
 $('pause').classList.toggle('hidden',!paused);
 keys.clear();
 if(paused&&document.pointerLockElement===canvas)document.exitPointerLock?.();
 if(!paused)requestMouseCapture();
}

function playSound(kind){if(state.mute)return;try{state.audio=state.audio||new (window.AudioContext||window.webkitAudioContext)();const ac=state.audio,osc=ac.createOscillator(),gain=ac.createGain(),now=ac.currentTime;osc.type=kind==='damage'?'sawtooth':kind==='collect'?'sine':kind==='hit'?'square':'triangle';let f={jump:320,dash:110,collect:620,hack:270,damage:100,hit:145,win:520,checkpoint:430,shoot:180,menu:320}[kind]||320;osc.frequency.setValueAtTime(f,now);osc.frequency.exponentialRampToValueAtTime(kind==='damage'?60:kind==='dash'?340:kind==='collect'?920:f*1.23,now+.16);gain.gain.setValueAtTime(.0001,now);gain.gain.exponentialRampToValueAtTime(.085,now+.012);gain.gain.exponentialRampToValueAtTime(.0001,now+.22);osc.connect(gain);gain.connect(ac.destination);osc.start(now);osc.stop(now+.24)}catch(e){}
}
// Original synthesized ambient soundtrack: generated at runtime with Web Audio.
const music={next:0,step:0};
function musicTick(){
  if(state.mute||!state.playing||state.paused||!state.audio)return;
  const ac=state.audio;
  if(ac.state==='suspended')return;
  if(ac.currentTime+0.05<music.next)return;
  let chords=[[110,164.81,220],[87.31,130.81,174.61],[98,146.83,196],[82.41,123.47,164.81]];
  let chord=chords[(Math.floor(music.step/2))%chords.length],start=ac.currentTime+.035;
  for(let k=0;k<3;k++){
    let osc=ac.createOscillator(),v=ac.createGain(),filter=ac.createBiquadFilter();
    osc.type=k===0?'sine':'triangle';osc.frequency.value=chord[k];
    filter.type='lowpass';filter.frequency.value=620;
    v.gain.setValueAtTime(.0001,start);v.gain.linearRampToValueAtTime(k===0?.018:.011,start+.4);
    v.gain.linearRampToValueAtTime(.0001,start+2.65);
    osc.connect(filter);filter.connect(v);v.connect(ac.destination);osc.start(start);osc.stop(start+2.75);
  }
  if(music.step%2===0){let o=ac.createOscillator(),g=ac.createGain();o.type='sine';o.frequency.setValueAtTime(state.region>=2?72:58,start);o.frequency.exponentialRampToValueAtTime(35,start+.22);g.gain.setValueAtTime(.015,start);g.gain.exponentialRampToValueAtTime(.0001,start+.35);o.connect(g);g.connect(ac.destination);o.start(start);o.stop(start+.36)}
  music.next=start+2.2;music.step++;
}
function message(text,seconds=3){$('subtitles').innerHTML='<strong>BIT //</strong> '+text;state.subtitleT=seconds}
function toast(text,seconds=2.5){$('toast').textContent=text;$('toast').classList.remove('hidden');state.toastT=seconds}
function particles(x,y,z,color,num=16,power=5){for(let i=0;i<num;i++){const a=Math.random()*Math.PI*2,b=Math.random()*2-1,r=Math.sqrt(1-b*b),s=power*(.4+Math.random()*.6);state.particles.push({x,y,z,vx:Math.cos(a)*r*s,vy:b*s+2,vz:Math.sin(a)*r*s,life:.5+Math.random()*.6,max:1,c:color,size:.08+Math.random()*.12})}}
function damage(amount=1){if(player.invuln>0||state.finished)return;player.health=clamp(player.health-amount,0,3);player.invuln=1.3;state.shake=.55;state.impact=.45;playSound('damage');particles(player.x,player.y+1,player.z,C.pink,14,6);if(player.health<=0){state.deaths++;setTimeout(()=>{if(state.playing&&!state.finished)respawn()},250);message('Integridad agotada. Reiniciando desde el último checkpoint...',2)}else message('Cuidado Zero. Tu integridad está disminuyendo.',2.5)}
function respawn(){const p=checkpoints[player.checkpoint];player.x=p.x;player.z=p.z;player.y=p.y+.35;player.vx=player.vz=player.vy=0;player.health=3;player.invuln=1.5;player.dashCd=0;state.projectiles=[];toast('CHECKPOINT RESTAURADO',1.8)}
function reset(){player.x=0;player.y=0;player.z=-2;player.vx=player.vy=player.vz=0;player.facingYaw=0;camYaw=0;camPitch=.08;player.onGround=true;player.health=3;player.fragments=0;player.dashCd=0;player.dashT=0;player.invuln=0;player.checkpoint=0;player.trail=[];player.steps=0;state.playing=true;state.paused=false;state.finished=false;state.elapsed=0;music.next=0;music.step=0;state.deaths=0;state.enemyKills=0;state.particles=[];state.projectiles=[];state.region=0;boss.hp=3;boss.active=false;boss.defeated=false;boss.shield=true;boss.terminal=[false,false];boss.vulnerable=0;boss.hitCd=0;boss.shotCd=2.2;drones.forEach(d=>{d.alive=true;d.cd=2});crystals.forEach(c=>c.collected=false);terminals.forEach(t=>t.off=0);checkpoints.forEach(c=>c.active=false);checkpoints[0].active=true;cam[0]=0;cam[1]=4;cam[2]=-12;changeCamera(cameraMode);$('menu').classList.add('hidden');$('end').classList.add('hidden');$('pause').classList.add('hidden');$('hud').classList.remove('hidden');message('Zero, te escucho. Recolectá los 3 fragmentos y seguí hacia el Núcleo.',5);toast('SISTEMA REINICIADO · SECTOR 01',2.5);playSound('menu')}
function finish(){state.finished=true;state.playing=false;if(document.pointerLockElement===canvas)document.exitPointerLock?.();$('hud').classList.add('hidden');$('end').classList.remove('hidden');$('endEyebrow').textContent='CONEXIÓN ESTABLE · MISIÓN COMPLETA';$('endTitle').innerHTML='PROTOCOLO<br>RESTAURADO<span>_</span>';$('endBody').textContent='Zero alcanzó el portal de salida. El Guardián fue desactivado y el sistema conserva los fragmentos recuperados.';$('endStats').textContent=`TIEMPO ${Math.floor(state.elapsed/60).toString().padStart(2,'0')}:${Math.floor(state.elapsed%60).toString().padStart(2,'0')}   /   CAÍDAS ${state.deaths}   /   DRONES ${state.enemyKills}`;playSound('win');particles(0,2,143,C.aqua,120,16)}
function tryJump(){if(!state.playing||state.paused||state.finished)return;if(player.onGround||player.coyote>0){player.vy=11.8;player.y+=.02;player.onGround=false;player.coyote=0;playSound('jump');particles(player.x,player.y+.1,player.z,C.cyan,8,2)}}
function tryDash(){if(!state.playing||state.paused||state.finished||player.dashCd>0)return;const d=directionFromKeys();player.dashDir=d.active?[d.x,d.z]:[-Math.sin(camYaw),Math.cos(camYaw)];player.dashT=.27;player.dashCd=1.3;player.vy=Math.max(player.vy,1.5);playSound('dash');particles(player.x,player.y+1,player.z,C.aqua,22,7);state.shake=.12}
function nearestInteraction(){let list=[];for(let i=0;i<terminals.length;i++){let t=terminals[i];if(t.type==='boss'&&!boss.active||t.type==='boss'&&boss.defeated)continue;let d=dist2(player.x,player.z,t.x,t.z);if(d<4.5)list.push({type:'terminal',i,dist:d,name:t.name})}for(const n of [{x:3,z:6,id:0},{x:3,z:50,id:1}]){let d=dist2(player.x,player.z,n.x,n.z);if(d<4)list.push({type:'bit',i:n.id,dist:d,name:'HABLAR CON BIT'})}list.sort((a,b)=>a.dist-b.dist);return list[0]||null}
function tryHack(){if(!state.playing||state.paused||state.finished)return;let q=nearestInteraction();if(!q){message('No hay un sistema hackeable cerca.',1.6);return}if(q.type==='bit'){message(q.i===0?'Soy Bit, tu guía. Recuperá los fragmentos marcados con luz amarilla.':'El Núcleo está adelante. Las dos torres de seguridad controlan el escudo del Guardián.',4);playSound('hack');return}let t=terminals[q.i];if(t.type==='hazard'){t.off=12;toast('FIREWALL DESACTIVADO · 12 s',2);message('Buen trabajo. El firewall permanecerá desactivado por unos segundos.',2.5)}else{let idx=q.i-1;if(boss.terminal[idx]){toast('NODO YA HACKEADO',1.4);return}if(player.fragments<3){message('Necesitamos los tres fragmentos para hackear el escudo central.',3);return}boss.terminal[idx]=true;toast(`${t.name} · ACCESO CONCEDIDO`,2.5);if(boss.terminal.every(Boolean)){boss.shield=false;boss.vulnerable=16;message('¡Escudo caído! Usá DASH contra el núcleo del Guardián.',3.5);toast('GUARDIÁN VULNERABLE · IMPACTO DASH',3);particles(0,3,123,C.pink,35,10)}else message('Queda un nodo de seguridad. Hackealo para bajar el escudo.',3)}playSound('hack');particles(t.x,1.4,t.z,C.cyan,26,7)}
function terrainAt(x,z){let found=null;for(const p of platforms){if(Math.abs(x-p.x)<=p.w/2-.18&&Math.abs(z-p.z)<=p.l/2-.18){if(!found||p.y>found.y)found=p}}return found}
function tick(dt){frameT+=dt;state.toastT-=dt;state.subtitleT-=dt;if(state.subtitleT<0)$('subtitles').style.opacity='0';else $('subtitles').style.opacity='1';if(state.toastT<=0)$('toast').classList.add('hidden');state.impact=Math.max(0,state.impact-dt*2.2);state.shake=Math.max(0,state.shake-dt*1.2);for(let p of state.particles){p.life-=dt;p.x+=p.vx*dt;p.y+=p.vy*dt;p.z+=p.vz*dt;p.vy-=8*dt}state.particles=state.particles.filter(p=>p.life>0).slice(-300);
 if(!state.playing||state.paused||state.finished)return;
 state.elapsed+=dt;musicTick();player.invuln=Math.max(0,player.invuln-dt);player.dashCd=Math.max(0,player.dashCd-dt);player.dashT=Math.max(0,player.dashT-dt);boss.hitCd=Math.max(0,boss.hitCd-dt);
 const move=directionFromKeys();player.facingYaw=-camYaw;
 if(player.dashT>0){player.vx=player.dashDir[0]*32;player.vz=player.dashDir[1]*32;player.vy=Math.max(player.vy,-1)}else{let accel=player.onGround?13:8,t=clamp(accel*dt,0,1);player.vx=lerp(player.vx,move.x*9.3,t);player.vz=lerp(player.vz,move.z*9.3,t)}
 let oldY=player.y;player.x+=player.vx*dt;player.z+=player.vz*dt;player.vy-=25*dt;player.y+=player.vy*dt;
 let found=terrainAt(player.x,player.z);let wasGround=player.onGround;player.onGround=false;
 if(found&&player.vy<=0&&player.y<=found.y+.04&&oldY>=found.y-.45){player.y=found.y;player.vy=0;player.onGround=true;player.coyote=.11;if(!wasGround&&Math.abs(player.vz)>4)particles(player.x,player.y+.1,player.z,C.cyan,5,1.6)}else{player.coyote=Math.max(0,player.coyote-dt)}
 if(player.y<-11){state.deaths++;respawn();message('Caíste al vacío. Reintentá desde el checkpoint.',2.7)}
 if(player.z>45&&state.region===0){state.region=1;toast('SECTOR 02 // MEMORIA FRAGMENTADA',3);message('La memoria está rota. No confíes en todo lo que ves.',4)}
 if(player.z>89&&state.region===1){state.region=2;toast('SECTOR 03 // NÚCLEO CORRUPTO',3);message('El Guardián está cerca. Vas a necesitar hackear sus nodos.',4)}
 for(let i=0;i<crystals.length;i++){let c=crystals[i];if(!c.collected&&dist2(player.x,player.z,c.x,c.z)<2.3&&Math.abs(player.y-c.y)<3){c.collected=true;player.fragments++;playSound('collect');toast(`FRAGMENTO RECUPERADO  ${player.fragments}/3`,2.3);message(player.fragments===3?'Los tres fragmentos están seguros. Avanzá hasta el Guardián.':'Código recuperado. Seguí buscando los demás fragmentos.',2.8);particles(c.x,c.y+1.6,c.z,C.yellow,35,7)}}
 for(let i=0;i<checkpoints.length;i++){let c=checkpoints[i];if(!c.active&&dist2(player.x,player.z,c.x,c.z)<2.7){c.active=true;player.checkpoint=i;player.health=3;playSound('checkpoint');toast('CHECKPOINT GUARDADO',2);particles(c.x,c.y+.3,c.z,C.green,30,6)}}
 for(let i=0;i<terminals.length;i++){if(terminals[i].off>0)terminals[i].off-=dt}
 for(let h of hazards){if(h.section===1&&terminals[0].off>0)continue;let dx=Math.abs(player.x-h.x),dz=Math.abs(player.z-h.z);if(dx<h.w/2&&dz<.6&&player.y<1.8+Math.sin(frameT*3)*.4){damage(1)}}
 let it=nearestInteraction();$('interact').classList.toggle('hidden',!it);if(it)$('interactText').textContent=it.type==='bit'?'HABLAR CON BIT':it.name;
 // Autonomous drone AI and projectiles
 for(let dr of drones){if(!dr.alive)continue;dr.x=dr.bx+Math.sin(frameT*dr.rate+dr.phase)*dr.amp;dr.z=dr.bz+Math.cos(frameT*dr.rate*.6+dr.phase)*.6;dr.cd-=dt;let dd=dist2(player.x,player.z,dr.x,dr.z);if(dd<2.2&&Math.abs(player.y-1.65)<2.3){if(player.dashT>0){dr.alive=false;state.enemyKills++;particles(dr.x,1.7,dr.z,C.pink,28,7);playSound('hit');toast('DRON NEUTRALIZADO',1.7)}else damage(1)}if(dd<12&&dr.cd<=0){dr.cd=2.4+Math.random()*.7;fire(dr.x,1.7,dr.z,player.x,player.y+1,player.z,false)}}
 if(!boss.active&&player.z>106){boss.active=true;toast('ALERTA // GUARDIÁN ACTIVADO',3);message('Los terminales Alfa y Beta controlan su escudo. Presioná E cuando estés cerca.',5)}
 if(boss.active&&!boss.defeated){boss.spin+=dt;boss.shotCd-=dt;if(!boss.shield){boss.vulnerable-=dt;if(boss.vulnerable<=0){boss.shield=true;boss.terminal=[false,false];toast('ESCUDO DEL GUARDIÁN RESTABLECIDO',2);message('El escudo volvió. Hackeá ambos nodos para desactivarlo.',2.8)}}if(boss.shotCd<=0&&player.z>105){boss.shotCd=boss.hp===1?1.35:2.15;fire(0,3.2,123,player.x,player.y+1,player.z,true);if(boss.hp<3){fire(0,3.2,123,player.x+2,player.y+1,player.z,true)}}let dd=dist2(player.x,player.z,0,123);if(dd<4.4&&Math.abs(player.y-1.3)<4.0&&boss.hitCd<=0){if(player.dashT>0&&!boss.shield){boss.hp--;boss.hitCd=1.6;boss.shield=true;boss.terminal=[false,false];boss.vulnerable=0;particles(0,3.1,123,C.pink,76,12);playSound('hit');state.shake=.6;toast(`GUARDIÁN DAÑADO · VIDA ${boss.hp}/3`,2.6);if(boss.hp<=0){boss.defeated=true;boss.active=false;message('¡Lo lograste! El Núcleo está libre. Entrá al portal de salida.',4);toast('GUARDIÁN DESACTIVADO // PORTAL ABIERTO',4)}else message('Impacto confirmado. Necesitás desactivar el escudo de nuevo.',3)}else if(player.dashT<=0)damage(1)}}
 for(let pr of state.projectiles){if(pr.dead)continue;pr.life-=dt;pr.x+=pr.vx*dt;pr.y+=pr.vy*dt;pr.z+=pr.vz*dt;if(pr.life<0){pr.dead=true;continue}let d=Math.hypot(player.x-pr.x,player.y+1-pr.y,player.z-pr.z);if(d<1.1){pr.dead=true;if(player.dashT>0){particles(pr.x,pr.y,pr.z,C.cyan,6,3)}else damage(1)}};state.projectiles=state.projectiles.filter(p=>!p.dead);
 if(boss.defeated&&player.z>141&&Math.abs(player.x)<4)finish();
 player.steps+=dt*Math.hypot(player.vx,player.vz);if(player.dashT>0){player.trail.push({x:player.x,y:player.y,z:player.z,life:.24})}for(let t of player.trail)t.life-=dt;player.trail=player.trail.filter(t=>t.life>0);
 updateHUD();
}
function fire(x,y,z,X,Y,Z,big){let dx=X-x,dy=Y-y,dz=Z-z,n=Math.hypot(dx,dy,dz)||1,s=big?13:10;state.projectiles.push({x,y,z,vx:dx/n*s,vy:dy/n*s,vz:dz/n*s,life:4,big,dead:false});playSound('shoot')}
function updateHUD(){let regionName=player.z<44?'SECTOR DE ARRANQUE':player.z<89?'MEMORIA FRAGMENTADA':'NÚCLEO CORRUPTO';$('zoneName').textContent=regionName;$('chapterLabel').textContent=(state.region+1).toString().padStart(2,'0');$('heartText').textContent=`${player.health} / 3`;$('healthFill').style.width=(player.health/3*100)+'%';$('fragmentsLabel').textContent=`${player.fragments} / 3`;$('dashFill').style.width=((1-player.dashCd/1.3)*100)+'%';$('dashText').textContent=player.dashCd<=0?'LISTO':player.dashCd.toFixed(1)+'s';$('bossHud').classList.toggle('hidden',!(boss.active&&!boss.defeated));$('bossFill').style.width=(boss.hp/3*100)+'%';$('bossStatus').textContent=boss.shield?`ESCUDO EN LÍNEA  //  HACK ${boss.terminal.filter(Boolean).length}/2 NODOS`:`ESCUDO CAÍDO  //  DASH PARA IMPACTAR  (${Math.ceil(boss.vulnerable)}s)`;$('objective').textContent=player.fragments<3?'Recupera los Fragmentos de Código ('+player.fragments+'/3)':!boss.defeated?'Desactiva al Guardián (hack + dash)':'Escapa por el portal al final del Núcleo';let target=player.fragments<3?crystals.filter(c=>!c.collected).sort((a,b)=>dist2(player.x,player.z,a.x,a.z)-dist2(player.x,player.z,b.x,b.z))[0]:(boss.defeated?{x:0,z:145}:(terminals.filter(t=>t.type==='boss'&&!boss.terminal[terminals.indexOf(t)-1]).sort((a,b)=>dist2(player.x,player.z,a.x,a.z)-dist2(player.x,player.z,b.x,b.z))[0]||{x:0,z:123}));if(target){let diff=target.x-player.x, forward=target.z-player.z,screenRight=-diff*Math.cos(camYaw)-forward*Math.sin(camYaw),screenForward=-diff*Math.sin(camYaw)+forward*Math.cos(camYaw),arrow=screenForward>1?(screenRight>2?'↗':screenRight< -2?'↖':'↑'):(screenForward< -1?'↓':screenRight>0?'→':'←');$('navHint').textContent=arrow+'  DESTINO  '+Math.round(Math.hypot(diff,forward))+' m'}$('screenFlash').style.opacity=state.impact*.65}
function resize(){let dpr=Math.min(window.devicePixelRatio||1,1.6),w=Math.floor(innerWidth*dpr),h=Math.floor(innerHeight*dpr);if(w!==screenW||h!==screenH){screenW=w;screenH=h;canvas.width=w;canvas.height=h;gl.viewport(0,0,w,h)}}
function render(){resize();gl.clearColor(.018,.035,.067,1);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);gl.enable(gl.DEPTH_TEST);gl.enable(gl.CULL_FACE);gl.cullFace(gl.BACK);gl.depthFunc(gl.LEQUAL);gl.useProgram(prog);let inGame=state.playing||state.finished;
 let follow=inGame?player:{x:0,y:0,z:12};
 let wobble=state.shake>0?Math.sin(frameT*60)*state.shake*.23:0;
 const sy=Math.sin(camYaw),cy=Math.cos(camYaw),sp=Math.sin(camPitch),cp=Math.cos(camPitch);
 let eye,tar;
 if(!inGame){
   eye=[12,12,-20];tar=[0,2,14];
 }else if(cameraMode==='first'){
   // Eye-height perspective. No player mesh in front of this camera.
   eye=[follow.x+wobble,follow.y+1.72,follow.z];
   tar=[eye[0]-sy*cp*24,eye[1]+sp*24,eye[2]+cy*cp*24];
 }else{
   // Freely orbiting chase camera. Its forward vector matches the input vector.
   eye=[follow.x+sy*camZoom+wobble,follow.y+Math.max(1.25,3.0-sp*camZoom*.53),follow.z-cy*camZoom];
   tar=[follow.x-sy*4.2,follow.y+1.55+sp*5.5,follow.z+cy*4.2];
 }
 const smooth=cameraMode==='first'&&inGame?1:.17;
 cam=cam.map((v,i)=>lerp(v,eye[i],smooth));
 cameraTarget=cameraTarget.map((v,i)=>lerp(v,tar[i],smooth));
 viewProj=mul(pers(Math.PI/(cameraMode==='first'&&inGame?2.85:3.05),screenW/screenH,.065,310),look(cam,cameraTarget));gl.uniform3fv(U.uCam,cam);gl.uniform1f(U.uTime,frameT);
 // Distant starfield, subsurface cyber grid and monumental horizon
 for(const star of stars){if(star.z>player.z-145&&star.z<player.z+215)draw('cube',star.x,star.y,star.z,star.sz,star.sz,star.sz,C.aqua,.8)}
 box(0,-26,86,260,.3,390,C.deep,.05,1);
 for(let x=-100;x<=100;x+=10)box(x,-25.7,85,.05,.06,380,C.cyan,.24);
 for(let z=-60;z<=260;z+=10)box(0,-25.7,z,200,.06,.045,C.cyan,.20);
 for(const p of bgStructures){box(p.x,p.y,p.z,p.w,p.h,p.w,C.deep,.025);box(p.x,p.y+p.h*.2,p.z,p.w+.12,.1,p.w+.12,p.x>0?C.violet:C.cyan,.33)}
 // Far center beacon, light column
 draw('sphere',0,65,210,33,33,33,C.violet,.19);draw('torus',0,64,208,60,60,6,C.cyan,.5,0,.23,0,Math.sin(frameT*.1)*.2);
 // Platforms with glowing edge and downward supports
 for(let p of platforms){if(Math.abs(p.z-player.z)>100)continue;
   let c=p.arena?C.plate2:p.section===2?C.plate:C.deep;
   box(p.x,p.y-.76,p.z,p.w,1.6,p.l,c,.03,1);
   box(p.x,p.y+.035,p.z,p.w,.10,p.l,C.plate2,.045,1);
   box(p.x-p.w/2+.06,p.y+.09,p.z,.11,.12,p.l,C.cyan,.48);
   box(p.x+p.w/2-.06,p.y+.09,p.z,.11,.12,p.l,C.cyan,.48);
   box(p.x,p.y+.09,p.z-p.l/2+.05,p.w,.12,.10,C.cyan,.43);
   box(p.x,p.y+.09,p.z+p.l/2-.05,p.w,.12,.10,C.cyan,.43);
   box(p.x,p.y-2.4,p.z,p.w*.66,1.8,p.l*.65,C.ink,.06);
   if(p.arena){for(let j=0;j<6;j++){let x=j%2===0?-p.w/2+1:p.w/2-1,z=110+Math.floor(j/2)*11;box(x,1.3,z,.8,2.6,.8,C.plate2,.06);box(x,2.65,z,1,.12,1,C.violet,.7)}}
   else{for(let side of [-1,1]){let x=p.x+side*(p.w/2-.65);box(x,p.y+.45,p.z-p.l/2+2,.33,.83,.33,C.plate2,.1);box(x,p.y+.89,p.z-p.l/2+2,.41,.05,.41,side>0?C.violet:C.cyan,.8)}}
 }
 // Digital pillars / debris alongside track
 for(let i=0;i<60;i++){let z=i*2.45-10,x=(i%2?1:-1)*(13+i%5*1.9),y=(i*17%9)*.35;box(x,y,z,.9,2+y,1.1,C.deep,.08);box(x,y+1.1,z,1,.09,1,(i%3?C.cyan:C.violet),.48)}
 // Mid-level security firewalls; disabled by hacking
 for(let h of hazards){let disabled=h.section===1&&terminals[0].off>0;let danger=disabled?C.green:C.pink;for(let i=0;i<4;i++){let xx=h.x-h.w/2+i*h.w/3;let yy=1.05+Math.sin(frameT*4+i*.6)*.13;box(xx,yy,h.z,.07,1.8,.15,danger,disabled?.14:.9);box(xx,2.03,h.z,.28,.15,.28,danger,.7)}box(h.x,2.07,h.z,h.w,.11,.13,danger,.65)}
 // Checkpoint pylons
 for(let i=0;i<checkpoints.length;i++){let q=checkpoints[i],active=q.active;box(q.x,q.y+.42,q.z,1.6,.75,1.6,C.plate2,.15);box(q.x,q.y+.83,q.z,.7,.08,.7,active?C.green:C.violet,.8);draw('torus',q.x,q.y+1.65+Math.sin(frameT*2+i)*.11,q.z,2,2,.5,active?C.green:C.violet,.7);draw('octa',q.x,q.y+1.62,q.z,.5,.65,.5,active?C.green:C.cyan,1.2,0,1,0,frameT*.3)}
 // Three data shards; idle rotation and glints
 for(let i=0;i<crystals.length;i++){let c=crystals[i];if(c.collected)continue;let yy=c.y+1.65+Math.sin(frameT*2.2+i)*.28;draw('octa',c.x,yy,c.z,1.6,2,1.6,C.yellow,1.15,0,1,frameT*1.1);draw('torus',c.x,yy,c.z,2.8,2.8,.6,C.yellow,.5,0,.32,0,frameT*.3);for(let j=0;j<5;j++){let a=j*6.28/5+frameT*.7;draw('cube',c.x+Math.cos(a)*1.25,yy+Math.sin(a*2)*.45,c.z+Math.sin(a)*1.25,.13,.13,.13,C.aqua,.9)}}
 // Terminals and hacking towers
 for(let i=0;i<terminals.length;i++){let q=terminals[i];if(q.type==='boss'&&!boss.active&&player.z<91)continue;let active=q.type==='hazard'?q.off>0:boss.terminal[i-1];let c=active?C.green:C.cyan;box(q.x,q.y+.9,q.z,1.65,1.8,1.5,C.plate2,.08);box(q.x,q.y+1.4,q.z+.76,1.2,.65,.09,c,.85);box(q.x,q.y+2.1,q.z,.42,.5,.38,c,1);draw('torus',q.x,q.y+2.8,q.z,1.5,1.5,.5,c,.8,0,.75);for(let j=0;j<4;j++)box(q.x-.6+j*.4,q.y+1.5,q.z+.83,.18,.1,.03,C.white,.5)}
 // Bit assistant NPC at stations, plus faint following guide light
 for(let n of [{x:3,z:6,y:0},{x:3,z:50,y:.05}]){let h=1.65+Math.sin(frameT*2+n.z)*.22;draw('sphere',n.x,n.y+h,n.z,1.0,.9,1.0,C.yellow,1.0);draw('torus',n.x,n.y+h,n.z,1.6,1.6,.4,C.yellow,.6,0,1,0,frameT*.2);box(n.x-.15,n.y+h+.06,n.z+.49,.13,.10,.08,C.ink,.05);box(n.x+.15,n.y+h+.06,n.z+.49,.13,.10,.08,C.ink,.05)}
 // Autonomous drones
 for(let dr of drones){if(!dr.alive)continue;let yy=1.9+Math.sin(frameT*2+dr.phase)*.3;draw('sphere',dr.x,yy,dr.z,1.7,1.55,1.7,C.plate2,.26);draw('torus',dr.x,yy,dr.z,2.45,2.45,.5,C.pink,1,0,1,frameT*2);draw('sphere',dr.x,yy,dr.z+.78,.5,.45,.2,C.pink,2);box(dr.x,yy-.45,dr.z,1.5,.13,1.5,C.pink,.6,0,1,frameT)}
 // Boss arena ambient rings and floating Guardian Core
 if(!boss.defeated){let yy=3.7+Math.sin(frameT*1.6)*.4,bc=boss.shield?C.pink:C.aqua;
 box(0,.3,123,5,.62,5,C.plate2,.18);for(let i=0;i<5;i++){let a=i*6.28/5+frameT*.35;draw('cube',Math.cos(a)*3,2.6+Math.sin(a)*.6,123+Math.sin(a)*3,1,2.3,.55,C.plate2,.2,0,1,a)}
 draw('sphere',0,yy,123,5,5,5,bc,.25);draw('sphere',0,yy,123.95,2.2,2.2,.25,boss.shield?C.pink:C.yellow,2.1);draw('torus',0,yy,123,8,8,1.2,bc,1.0,0,.85,frameT*.1);draw('torus',0,yy,123,6.3,6.3,.7,C.violet,.8,0,.7,frameT*.25);draw('octa',0,yy,123,3.5,4.9,3.5,C.ink,.23,0,1,frameT*.55);if(boss.shield){draw('sphere',0,yy,123,9,9,9,C.pink,.18,0,.115)}}else{draw('torus',0,2.8,123,6,6,.8,C.green,.8)}
 // Exit portal
 let portalC=boss.defeated?C.aqua:C.violet;box(0,.35,145,7,.7,4,C.plate2,.1);for(let i=0;i<2;i++)draw('torus',0,4,145,9+i*1.2,9+i*1.2,.8,portalC,.9,0,1,0,Math.sin(frameT*.6)*.08);draw('sphere',0,4,145,6.8,6.8,.7,portalC,.15,0,boss.defeated?.38:.12);
 // Glow motes, projectiles and particle fragments
 for(let p of state.projectiles){draw('sphere',p.x,p.y,p.z,p.big?.9:.52,p.big?.9:.52,p.big?.9:.52,p.big?C.pink:C.violet,1.8);draw('torus',p.x,p.y,p.z,p.big?2:1.25,p.big?2:1.25,.5,p.big?C.pink:C.violet,.5,0,.6)}
 for(let p of state.particles){let a=clamp(p.life/.8,0,1);draw('cube',p.x,p.y,p.z,p.size*a,p.size*a,p.size*a,p.c,1.2)}
 // Shadow; in FPS the avatar is hidden so no geometry obstructs the view.
 draw('sphere',player.x,player.y+.035,player.z,1.4,.07,1.4,C.ink,.2,0,.55);
 if(cameraMode==='third'||!state.playing){
   for(let t of player.trail){draw('sphere',t.x,t.y+1.25,t.z,1.05,1.8,.8,C.cyan,.5,0,t.life/.24*.19)}
   if(player.invuln<=0||Math.floor(frameT*17)%3!==0){
      let yy=player.y+(player.onGround?Math.sin(player.steps*.55)*.065:0);
      let run=Math.hypot(player.vx,player.vz)>2;
      let swing=run&&player.onGround?Math.sin(player.steps*1.3)*.29:0;
      const cs=Math.cos(player.facingYaw),sn=Math.sin(player.facingYaw);
      const part=(dx,dy,dz,w,h,l,color,emit=0)=>{
        let xx=player.x+dx*cs+dz*sn,zz=player.z-dx*sn+dz*cs;
        box(xx,yy+dy,zz,w,h,l,color,emit,0,1,player.facingYaw);
      };
      part(0,1.05,0,1.1,1.25,.83,C.deep,.08);
      part(0,1.78,0,.96,.73,.82,C.plate2,.16);
      part(0,1.82,.44,.82,.22,.10,C.cyan,1.8);
      part(-.68,1.05,0,.23,.9,.36,C.plate2,.12);
      part(.68,1.05,0,.23,.9,.36,C.plate2,.12);
      part(-.31,.34,swing,.35,.72,.45,C.plate2,.04);
      part(.31,.34,-swing,.35,.72,.45,C.plate2,.04);
      part(0,1.05,-.49,.64,.43,.2,C.violet,1.1);
      draw('torus',player.x,yy+1.12,player.z,2.4,2.4,.5,C.cyan,.6,0,.38,0,frameT*.15);
      if(player.dashT>0)draw('sphere',player.x,yy+1.3,player.z,2.7,3.1,2.7,C.aqua,.35,0,.17);
   }
 }

}
window.addEventListener('keydown',e=>{
  if(['Space','ShiftLeft','ShiftRight','ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.code))e.preventDefault();
  if(e.repeat)return;
  keys.add(e.code);
  if(e.code==='KeyV'||e.code==='KeyC'){changeCamera(cameraMode==='third'?'first':'third');if(state.playing&&!state.paused)toast(cameraMode==='first'?'CÁMARA · 1ª PERSONA':'CÁMARA · 3ª PERSONA',1.2);return}
  if(e.code==='Space')tryJump();
  if(e.code==='ShiftLeft'||e.code==='ShiftRight')tryDash();
  if(e.code==='KeyE')tryHack();
  if(e.code==='KeyM'){state.mute=!state.mute;toast(state.mute?'AUDIO SILENCIADO':'AUDIO ACTIVADO',1.5)}
  if(e.code==='Escape'&&state.playing&&!state.finished&&!state.paused)setPaused(true);
});
window.addEventListener('keyup',e=>keys.delete(e.code));
window.addEventListener('blur',()=>keys.clear());
canvas.addEventListener('pointerdown',e=>{
 if(!state.playing||state.paused||state.finished)return;
 if(document.pointerLockElement!==canvas){drag=true;dragXY=[e.clientX,e.clientY];requestMouseCapture()}
});
window.addEventListener('pointerup',()=>drag=false);
document.addEventListener('mousemove',e=>{
 if(!state.playing||state.paused||state.finished)return;
 if(document.pointerLockElement===canvas){applyMouse(e.movementX||0,e.movementY||0)}
 else if(drag){applyMouse(e.clientX-dragXY[0],e.clientY-dragXY[1]);dragXY=[e.clientX,e.clientY]}
});
document.addEventListener('pointerlockchange',()=>{
 const locked=document.pointerLockElement===canvas;
 $('mouseStatus').textContent=locked?'MOUSE ACTIVO':'CLICK EN EL JUEGO PARA MIRAR';
 if(!locked&&state.playing&&!state.paused&&!state.finished&&!drag){
   // The browser can release capture on Esc without delivering a keydown.
   setPaused(true);
 }
 if(locked)drag=false;
});
window.addEventListener('wheel',e=>{
 if(!state.playing||state.paused||cameraMode==='first')return;
 camZoom=clamp(camZoom+Math.sign(e.deltaY)*1.2,5.5,17.5);
},{passive:true});
$('startButton').onclick=()=>{reset();requestMouseCapture()};
$('replayButton').onclick=()=>{reset();requestMouseCapture()};
$('resumeButton').onclick=()=>setPaused(false);
$('restartFromPause').onclick=()=>{reset();requestMouseCapture()};
$('cameraToggle').onclick=()=>{changeCamera(cameraMode==='third'?'first':'third');requestMouseCapture()};

// Public, read-only QA hook, allows reproducible end-state screenshots without external libs.
window.__NIVEL_CERO_QA__={snapshot:()=>({x:player.x,z:player.z,y:player.y,hp:player.health,fragments:player.fragments,bossHP:boss.hp,shield:boss.shield,playing:state.playing,finished:state.finished,crystals:crystals.map(c=>!!c.collected),fps:fpsSmooth,cameraMode,camYaw,camPitch,cam:[...cam],faceYaw:player.facingYaw}),setCameraMode:(m)=>changeCamera(m),setYaw:(rad)=>{camYaw=rad;player.facingYaw=-camYaw},step:(n=12)=>{for(let i=0;i<n;i++)tick(1/60)},moveTo:(x,z,y)=>{player.x=x;player.z=z;player.y=y??terrainAt(x,z)?.y??0;player.vx=player.vy=player.vz=0},giveFragments:()=>{crystals.forEach(c=>c.collected=true);player.fragments=3},bossVulnerable:()=>{boss.active=true;boss.shield=false;boss.vulnerable=60},defeatBoss:()=>{boss.defeated=true;boss.active=false},toggleMute:()=>state.mute=!state.mute};
function animate(now){const dt=clamp((now-last)/1000,0,.04);last=now;fpsSmooth=lerp(fpsSmooth,dt>0?1/dt:60,.05);if(Math.floor(now/1000)%2===0)$('fpsLabel').textContent=Math.min(144,Math.round(fpsSmooth));tick(dt);render();requestAnimationFrame(animate)}
updateHUD();requestAnimationFrame(animate);
})();
