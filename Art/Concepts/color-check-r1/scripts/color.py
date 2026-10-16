import numpy as np
def srgb2lin(c):
    c=np.asarray(c,float)/255
    return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def lin2srgb(l):
    l=np.clip(l,0,1); return np.where(l<=0.0031308,12.92*l,1.055*l**(1/2.4)-0.055)*255
def hex2rgb(h): h=h.lstrip('#'); return [int(h[i:i+2],16) for i in (0,2,4)]
def rgb2lab(rgb):
    l=srgb2lin(rgb); M=np.array([[0.4124,0.3576,0.1805],[0.2126,0.7152,0.0722],[0.0193,0.1192,0.9505]])
    xyz=l@M.T/np.array([0.95047,1.0,1.08883])
    f=np.where(xyz>0.008856,np.cbrt(xyz),7.787*xyz+16/116)
    return np.stack([116*f[...,1]-16,500*(f[...,0]-f[...,1]),200*(f[...,1]-f[...,2])],-1)
def de2000(a,b):
    L1,a1,b1=a; L2,a2,b2=b
    C1=np.hypot(a1,b1);C2=np.hypot(a2,b2);Cb=(C1+C2)/2
    G=0.5*(1-np.sqrt(Cb**7/(Cb**7+25**7)))
    a1p=(1+G)*a1;a2p=(1+G)*a2;C1p=np.hypot(a1p,b1);C2p=np.hypot(a2p,b2)
    h1p=np.degrees(np.arctan2(b1,a1p))%360;h2p=np.degrees(np.arctan2(b2,a2p))%360
    dLp=L2-L1;dCp=C2p-C1p
    dh=h2p-h1p
    if C1p*C2p==0: dh=0
    elif dh>180: dh-=360
    elif dh<-180: dh+=360
    dHp=2*np.sqrt(C1p*C2p)*np.sin(np.radians(dh/2))
    Lbp=(L1+L2)/2;Cbp=(C1p+C2p)/2
    if C1p*C2p==0: hbp=h1p+h2p
    elif abs(h1p-h2p)<=180: hbp=(h1p+h2p)/2
    elif h1p+h2p<360: hbp=(h1p+h2p+360)/2
    else: hbp=(h1p+h2p-360)/2
    T=1-0.17*np.cos(np.radians(hbp-30))+0.24*np.cos(np.radians(2*hbp))+0.32*np.cos(np.radians(3*hbp+6))-0.20*np.cos(np.radians(4*hbp-63))
    dth=30*np.exp(-((hbp-275)/25)**2);RC=2*np.sqrt(Cbp**7/(Cbp**7+25**7))
    SL=1+0.015*(Lbp-50)**2/np.sqrt(20+(Lbp-50)**2);SC=1+0.045*Cbp;SH=1+0.015*Cbp*T;RT=-np.sin(np.radians(2*dth))*RC
    return float(np.sqrt((dLp/SL)**2+(dCp/SC)**2+(dHp/SH)**2+RT*(dCp/SC)*(dHp/SH)))
# Machado et al. 2009, severity 1.0
CVD={'protan':np.array([[0.152286,1.052583,-0.204868],[0.114503,0.786281,0.099216],[-0.003882,-0.048116,1.051998]]),
     'deutan':np.array([[0.367322,0.860646,-0.227968],[0.280085,0.672501,0.047413],[-0.011820,0.042940,0.968881]]),
     'tritan':np.array([[1.255528,-0.076749,-0.178779],[-0.078411,0.930809,0.147602],[0.004733,0.691367,0.303900]])}
def simulate(rgb,kind):
    if kind=='normal': return np.asarray(rgb,float)
    l=srgb2lin(rgb); return lin2srgb(l@CVD[kind].T)
def gray(rgb):
    l=srgb2lin(rgb); y=l@np.array([0.2126,0.7152,0.0722]); return lin2srgb(np.stack([y,y,y],-1))
