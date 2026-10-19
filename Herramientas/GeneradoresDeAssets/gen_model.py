"""Converts the Poly Haven monobloc chair (glTF, CC0) into:
   - Resources/Models/chair.bytes  (compact binary mesh, loaded at runtime by MeshLoader.cs)
   - Art/Models/chair.obj          (same mesh, for reference / editing)
Converts glTF right-handed coords to Unity left-handed (negate X, flip winding) and flips V.
Also duplicates every triangle with reversed winding because the source material is double-sided."""
import json, struct, sys, os
import numpy as np

src, out_bytes, out_obj = sys.argv[1], sys.argv[2], sys.argv[3]
g = json.load(open(src))
binp = os.path.join(os.path.dirname(src), g["buffers"][0]["uri"])
buf = open(binp, "rb").read()

def acc(i):
    a = g["accessors"][i]; bv = g["bufferViews"][a["bufferView"]]
    off = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
    comps = {"SCALAR": 1, "VEC2": 2, "VEC3": 3}[a["type"]]
    dt = {5126: np.float32, 5123: np.uint16, 5125: np.uint32}[a["componentType"]]
    return np.frombuffer(buf, dtype=dt, count=a["count"] * comps, offset=off).reshape(a["count"], comps) if comps > 1 \
        else np.frombuffer(buf, dtype=dt, count=a["count"], offset=off)

prim = g["meshes"][0]["primitives"][0]
P = acc(prim["attributes"]["POSITION"]).astype(np.float32).copy()
N = acc(prim["attributes"]["NORMAL"]).astype(np.float32).copy()
UV = acc(prim["attributes"]["TEXCOORD_0"]).astype(np.float32).copy()
I = acc(prim["indices"]).astype(np.int32).reshape(-1, 3).copy()

P[:, 0] *= -1; N[:, 0] *= -1; UV[:, 1] = 1 - UV[:, 1]
I = I[:, [0, 2, 1]]                          # flip winding after mirroring

# Back faces: duplicate vertices with inverted normals and reversed winding.
nv = len(P)
P2 = np.vstack([P, P]); N2 = np.vstack([N, -N]); UV2 = np.vstack([UV, UV])
I2 = np.vstack([I, I[:, [0, 2, 1]] + nv])

back = P[P[:, 1] > 0.6]
print("verts", len(P2), "tris", len(I2), "backrest mean z", back[:, 2].mean(), "bounds", P.min(0), P.max(0))

os.makedirs(os.path.dirname(out_bytes), exist_ok=True)
with open(out_bytes, "wb") as f:
    f.write(b"NMDV"); f.write(struct.pack("<ii", len(P2), I2.size))
    f.write(P2.astype("<f4").tobytes()); f.write(N2.astype("<f4").tobytes())
    f.write(UV2.astype("<f4").tobytes()); f.write(I2.astype("<i4").tobytes())

os.makedirs(os.path.dirname(out_obj), exist_ok=True)
with open(out_obj, "w") as f:
    f.write("# Plastic Monobloc Chair 01 - Poly Haven (CC0) - https://polyhaven.com/a/plastic_monobloc_chair_01\n")
    f.write("# Original glTF coordinates (right-handed, Y up, metres)\n")
    Po = acc(prim["attributes"]["POSITION"]); No = acc(prim["attributes"]["NORMAL"]); Uo = acc(prim["attributes"]["TEXCOORD_0"])
    for p in Po: f.write("v %.5f %.5f %.5f\n" % tuple(p))
    for t in Uo: f.write("vt %.5f %.5f\n" % (t[0], 1 - t[1]))
    for n in No: f.write("vn %.5f %.5f %.5f\n" % tuple(n))
    f.write("o plastic_monobloc_chair_01\n")
    for a, b, c in acc(prim["indices"]).reshape(-1, 3) + 1:
        f.write(f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}\n")
print("model ok")
