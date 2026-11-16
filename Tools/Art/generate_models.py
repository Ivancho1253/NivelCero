"""Original Nivel Cero mechanical art. Blender 4.5 LTS, no external assets.

Run: blender --background --python Tools/Art/generate_models.py
Exports metre-scale FBX assets and editable .blend source to Unity Art/Models.
Every plate is authored here; meshes are deliberately stylized industrial art.
"""
from pathlib import Path
import json
import math
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Unity/Assets/NivelCero/Art/Models"
SOURCE = ROOT / "Tools/Art/Source"
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
MATS = {}
EXPORTED = []


def material(name, color, metallic=0.0, roughness=0.4, emission=0.0):
    mat = bpy.data.materials.new("NC_" + name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Emission Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Strength"].default_value = emission
    MATS[name] = mat
    return mat


def setup():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for data in list(bpy.data.materials):
        bpy.data.materials.remove(data)
    material("Armor", (.032, .045, .067), .82, .31)
    material("Titanium", (.15, .21, .28), .9, .29)
    material("Edge", (.31, .4, .48), .88, .25)
    material("Rubber", (.012, .018, .026), .08, .7)
    material("Cyan", (.025, .75, 1), .25, .21, 3.0)
    material("White", (.72, .85, .92), .45, .3, .1)
    material("Amber", (1, .38, .035), .35, .25, 2.5)
    material("Red", (1, .025, .11), .25, .25, 3.5)
    material("Violet", (.4, .09, 1), .35, .25, 2.5)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1
    bpy.context.scene.render.fps = 30


def finish(obj, name, mat, bone=None, rig=None, smooth=False):
    obj.name = name
    obj.data.materials.append(MATS[mat])
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    if bone and rig:
        group = obj.vertex_groups.new(name=bone)
        group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
        modifier = obj.modifiers.new("Zero mechanical skin", "ARMATURE")
        modifier.object = rig
        obj.parent = rig
    return obj


def bevel(obj, amount=.025, segments=3):
    mod = obj.modifiers.new("Machined rounded edges", "BEVEL")
    mod.width = amount
    mod.segments = segments
    mod.limit_method = "ANGLE"
    # Apply edge geometry before the armature modifier is assigned.
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def box(name, loc, size, mat="Armor", rounding=.025, bone=None, rig=None, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rounding:
        bevel(obj, min(rounding, min(size) * .34))
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, mat, bone, rig)


def sphere(name, loc, size, mat="Armor", bone=None, rig=None, segments=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=12, radius=1, location=loc)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, bone, rig, True)


def cylinder(name, start, end, radius, mat="Titanium", bone=None, rig=None, vertices=16, radius2=None):
    a, b = Vector(start), Vector(end)
    delta = b - a
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius,
        radius2=radius if radius2 is None else radius2, depth=delta.length, location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = delta.to_track_quat("Z", "Y")
    bevel(obj, min(.012, radius * .18), 2)
    return finish(obj, name, mat, bone, rig, True)


def ring(name, loc, radius, thickness, mat="Cyan", rotation=(0, 0, 0), bone=None, rig=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=40, minor_segments=8, location=loc,
        major_radius=radius, minor_radius=thickness, rotation=rotation)
    return finish(bpy.context.object, name, mat, bone, rig, True)


def plate(name, loc, width, depth, height, mat="Armor", bone=None, rig=None):
    # Eight-sided tapered armor, with a sloped top and faceted shoulders.
    w, d, h = width / 2, depth / 2, height / 2
    outline = [(-w * .72, -d), (w * .72, -d), (w, -d * .45), (w, d * .65),
               (w * .65, d), (-w * .65, d), (-w, d * .65), (-w, -d * .45)]
    vertices = [(x, y, -h) for x, y in outline]
    vertices += [(x * .88, y * .88, h) for x, y in outline]
    faces = [tuple(reversed(range(8))), tuple(range(8, 16))]
    faces += [(i, (i + 1) % 8, (i + 1) % 8 + 8, i + 8) for i in range(8)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    bevel(obj, .012)
    return finish(obj, name, mat, bone, rig)


def begin_asset():
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    return set(bpy.context.scene.objects)


def end_asset(name, previous, rig=None):
    objects = [o for o in bpy.context.scene.objects if o not in previous]
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    for obj in objects:
        for old_collection in list(obj.users_collection):
            old_collection.objects.unlink(obj)
        collection.objects.link(obj)
    if rig:
        # One skinned renderer with material submeshes, rather than one per armor plate.
        meshes = [o for o in objects if o.type == "MESH"]
        bpy.ops.object.select_all(action="DESELECT")
        for obj in meshes:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = name + "_SkinnedArmor"
        objects = [rig, joined]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig or objects[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name + ".fbx")), use_selection=True,
        object_types={"ARMATURE", "MESH"}, apply_unit_scale=True, global_scale=1,
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
        bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, armature_nodetype="NULL", use_armature_deform_only=False,
        bake_anim=bool(rig), bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=bool(rig), bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0, path_mode="AUTO", embed_textures=False)
    triangles = sum(sum(len(poly.vertices) - 2 for poly in o.data.polygons) for o in objects if o.type == "MESH")
    EXPORTED.append({"asset": name, "objects": len(objects), "triangles": triangles,
                     "rigged": bool(rig), "source": "Original procedural mesh, generate_models.py"})
    for obj in objects:
        obj.hide_set(True)


def make_rig():
    armature = bpy.data.armatures.new("Zero_Skeleton")
    obj = bpy.data.objects.new("Zero_Rig", armature)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = [
        ("Root", (0, 0, 0), (0, 0, .25), None),
        ("Pelvis", (0, 0, .94), (0, 0, 1.1), "Root"),
        ("Spine", (0, 0, 1.1), (0, 0, 1.3), "Pelvis"),
        ("Chest", (0, 0, 1.3), (0, 0, 1.48), "Spine"),
        ("Neck", (0, 0, 1.48), (0, 0, 1.58), "Chest"),
        ("Head", (0, 0, 1.58), (0, 0, 1.86), "Neck"),
    ]
    for side, sign in [("L", -1), ("R", 1)]:
        bones.extend([
            ("UpperArm." + side, (sign * .24, 0, 1.46), (sign * .4, 0, 1.24), "Chest"),
            ("Forearm." + side, (sign * .4, 0, 1.24), (sign * .49, -.025, 1.02), "UpperArm." + side),
            ("Hand." + side, (sign * .49, -.025, 1.02), (sign * .53, -.045, .93), "Forearm." + side),
            ("Thigh." + side, (sign * .135, 0, .94), (sign * .145, .015, .55), "Pelvis"),
            ("Shin." + side, (sign * .145, .015, .55), (sign * .145, 0, .16), "Thigh." + side),
            ("Foot." + side, (sign * .145, 0, .16), (sign * .145, -.19, .075), "Shin." + side),
        ])
    for name, head, tail, parent in bones:
        bone = armature.edit_bones.new(name)
        bone.head, bone.tail = head, tail
        if parent:
            bone.parent = armature.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    for bone in obj.pose.bones:
        bone.rotation_mode = "XYZ"
    return obj


def zero():
    previous = begin_asset()
    rig = make_rig()
    plate("PelvicArmor", (0, 0, .96), .36, .27, .22, "Titanium", "Pelvis", rig)
    for i in range(3):
        box("AbdominalFlex_%02d" % i, (0, -.035, 1.085 + i * .055), (.29 - i * .01, .25, .043),
            "Rubber", .012, "Spine", rig)
    plate("ThoraxShell", (0, .015, 1.35), .52, .32, .29, "Armor", "Chest", rig)
    for s in [-1, 1]:
        plate("PectoralArmor", (s * .125, -.16, 1.36), .23, .075, .23, "Titanium", "Chest", rig)
        box("ChestCyanMark", (s * .17, -.206, 1.39), (.026, .015, .15), "Cyan", .005, "Chest", rig)
    plate("SternumReactor", (0, -.185, 1.31), .09, .055, .17, "Edge", "Chest", rig)
    box("CoreStatus", (0, -.22, 1.32), (.035, .012, .095), "Cyan", .004, "Chest", rig)
    box("ZeroBackpack", (0, .22, 1.3), (.3, .16, .35), "Armor", .045, "Chest", rig)
    for s in [-1, 1]:
        cylinder("SpinalCell", (s * .09, .32, 1.16), (s * .09, .32, 1.44), .032, "Titanium", "Chest", rig)
        cylinder("SpinalCellGlow", (s * .09, .322, 1.2), (s * .09, .322, 1.4), .022, "Cyan", "Chest", rig)
    cylinder("NeckSeal", (0, 0, 1.48), (0, 0, 1.6), .092, "Rubber", "Neck", rig)
    sphere("Helmet", (0, -.006, 1.7), (.158, .148, .185), "Armor", "Head", rig)
    plate("HelmetBrow", (0, -.115, 1.79), .29, .1, .1, "Titanium", "Head", rig)
    box("VisorFrame", (0, -.144, 1.704), (.274, .035, .088), "Rubber", .025, "Head", rig)
    box("ContinuousVisor", (0, -.166, 1.709), (.252, .026, .047), "Cyan", .018, "Head", rig)
    plate("ChinGuard", (0, -.105, 1.608), .19, .11, .08, "Titanium", "Head", rig)
    for s in [-1, 1]:
        cylinder("HelmetAudio", (s * .13, 0, 1.7), (s * .175, 0, 1.7), .058, "Titanium", "Head", rig)
        cylinder("EarStatus", (s * .175, 0, 1.7), (s * .181, 0, 1.7), .025, "Cyan", "Head", rig)

    for side, s in [("L", -1), ("R", 1)]:
        upper, fore, hand = "UpperArm." + side, "Forearm." + side, "Hand." + side
        thigh, shin, foot = "Thigh." + side, "Shin." + side, "Foot." + side
        sphere("ShoulderJoint." + side, (s * .255, 0, 1.435), (.11, .108, .105), "Rubber", upper, rig)
        plate("ShoulderArmor." + side, (s * .285, -.005, 1.465), .2, .25, .155, "Titanium", upper, rig)
        cylinder("UpperArmFlex." + side, (s * .29, 0, 1.4), (s * .4, 0, 1.24), .069, "Rubber", upper, rig)
        plate("BicepArmor." + side, (s * .354, -.035, 1.32), .135, .16, .16, "Armor", upper, rig)
        sphere("Elbow." + side, (s * .4, 0, 1.24), (.067, .069, .068), "Edge", fore, rig)
        cylinder("ForearmArmor." + side, (s * .415, 0, 1.22), (s * .485, -.025, 1.04), .078,
                 "Titanium", fore, rig, radius2=.057)
        box("WristDisplay." + side, (s * .465, -.078, 1.11), (.067, .016, .085), "Cyan", .008, fore, rig)
        sphere("Glove." + side, (s * .51, -.03, .99), (.062, .063, .086), "Rubber", hand, rig)
        plate("Knuckle." + side, (s * .51, -.08, .99), .09, .035, .09, "Titanium", hand, rig)
        cylinder("ThighFlex." + side, (s * .135, 0, .92), (s * .145, .015, .57), .09, "Rubber", thigh, rig)
        plate("ThighArmor." + side, (s * .147, -.033, .76), .19, .2, .28, "Titanium", thigh, rig)
        box("ThighMark." + side, (s * .147, -.14, .77), (.022, .014, .18), "Cyan", .004, thigh, rig)
        sphere("KneeSeal." + side, (s * .145, .015, .55), (.083, .087, .083), "Rubber", shin, rig)
        plate("Kneepad." + side, (s * .145, -.075, .55), .17, .09, .15, "Edge", shin, rig)
        cylinder("LowerLegFlex." + side, (s * .145, .015, .5), (s * .145, 0, .18), .066, "Rubber", shin, rig)
        plate("ShinArmor." + side, (s * .145, -.025, .33), .16, .19, .27, "Armor", shin, rig)
        box("ShinEnergy." + side, (s * .145, -.13, .33), (.022, .012, .18), "Cyan", .004, shin, rig)
        box("Boot." + side, (s * .145, -.075, .095), (.19, .34, .16), "Titanium", .045, foot, rig)
        box("Sole." + side, (s * .145, -.075, .035), (.2, .35, .05), "Rubber", .015, foot, rig)
    animate_zero(rig)
    end_asset("Zero", previous, rig)


def animate_zero(rig):
    # All clips use an in-place Root. Rigid armor is weighted to its mechanical bone.
    rig.animation_data_create()

    def action(name, frames, poses, looping=False):
        clip = bpy.data.actions.new(name)
        clip.use_fake_user = True
        rig.animation_data.action = clip
        for frame in frames:
            for bone in rig.pose.bones:
                bone.rotation_euler = (0, 0, 0)
                bone.location = (0, 0, 0)
            poses(frame, frames[-1])
            for bone in rig.pose.bones:
                bone.keyframe_insert("rotation_euler", frame=frame, group=bone.name)
                bone.keyframe_insert("location", frame=frame, group=bone.name)
        # Explicit metadata also goes in manifest; Unity importer sets looping by clip name.
        clip["NC_Loop"] = looping
        clip["NC_InPlace"] = True

    def rot(name, x=0, y=0, z=0):
        rig.pose.bones[name].rotation_euler = tuple(math.radians(v) for v in (x, y, z))

    def idle(f, end):
        wave = math.sin((f - 1) / (end - 1) * math.tau)
        rot("Chest", 1.1 * wave)
        rot("Head", 0, 2 * wave)
        rig.pose.bones["Pelvis"].location.y = .006 * wave

    def locomotion(f, end, strength):
        cycle = (f - 1) / (end - 1) * math.tau
        wave = math.sin(cycle)
        for side, sign in [("L", -1), ("R", 1)]:
            stride = wave * sign
            rot("Thigh." + side, stride * strength)
            rot("Shin." + side, max(0, -stride) * strength * 1.5)
            rot("Foot." + side, -stride * strength * .25)
            rot("UpperArm." + side, -stride * strength * .65)
            rot("Forearm." + side, -12 - max(0, stride) * strength * .3)
        rot("Chest", -strength * .08, wave * 3)
        rig.pose.bones["Pelvis"].location.y = abs(math.sin(cycle)) * .025

    action("Idle", [1, 16, 31, 46, 61], idle, True)
    action("Walk", [1, 5, 9, 13, 17, 21, 25, 29, 33], lambda f, e: locomotion(f, e, 25), True)
    action("Run", [1, 4, 7, 10, 13, 16, 19, 22, 25], lambda f, e: locomotion(f, e, 43), True)

    def jump(f, end):
        a = math.sin((f - 1) / (end - 1) * math.pi)
        rot("Thigh.L", -35 * a); rot("Thigh.R", -20 * a)
        rot("Shin.L", 65 * a); rot("Shin.R", 45 * a)
        rot("UpperArm.L", -40 * a); rot("UpperArm.R", -40 * a)
        rot("Chest", -8 * a)
    action("Jump", [1, 5, 10, 16, 22], jump)

    def fall(f, end):
        a = .8 + math.sin(f / end * math.tau) * .08
        rot("Thigh.L", -16 * a); rot("Thigh.R", 12 * a)
        rot("Shin.L", 32 * a); rot("Shin.R", 30 * a)
        rot("UpperArm.L", -30, 0, -12); rot("UpperArm.R", -30, 0, 12)
    action("Fall", [1, 10, 19], fall, True)

    def dash(f, end):
        a = math.sin((f - 1) / (end - 1) * math.pi)
        rot("Chest", 22 * a); rot("Thigh.L", -48 * a); rot("Thigh.R", 30 * a)
        rot("Shin.L", 60 * a); rot("Shin.R", 35 * a)
        rot("UpperArm.L", 32 * a); rot("UpperArm.R", 32 * a)
    action("Dash", [1, 3, 6, 10], dash)

    def shoot(f, end):
        recoil = math.sin((f - 1) / (end - 1) * math.pi)
        rot("UpperArm.R", -70 - recoil * 7, 0, -10)
        rot("Forearm.R", -24, 0, 0); rot("Chest", -recoil * 3)
        rot("UpperArm.L", -35, 0, 15); rot("Forearm.L", -55)
    action("Shoot", [1, 3, 5, 8], shoot)

    def reload(f, end):
        a = math.sin((f - 1) / (end - 1) * math.pi)
        rot("UpperArm.R", -55, 10, -20); rot("Forearm.R", -40)
        rot("UpperArm.L", -35 - a * 25, 0, 25); rot("Forearm.L", -25 - a * 55)
        rot("Head", 12 * a, -10 * a)
    action("Reload", [1, 8, 16, 24, 32, 39], reload)

    def melee(f, end, variant):
        t = (f - 1) / (end - 1)
        strike = math.sin(t * math.pi)
        twist = (math.sin(t * math.pi * 2) * (26 if variant < 3 else 8))
        rot("Chest", (16 if variant == 3 else 2) * strike, twist * (-1 if variant == 2 else 1))
        rot("UpperArm.R", -95 * strike, twist, (-65 if variant == 2 else 65) * strike)
        rot("Forearm.R", -30 * strike)
        rot("UpperArm.L", 15 * strike, 0, -15 * strike)
        rot("Thigh.L", -12 * strike); rot("Thigh.R", 10 * strike)
    for index in range(1, 4):
        action("Melee%d" % index, [1, 4, 7, 10, 14, 18], lambda f, e, v=index: melee(f, e, v))

    def hurt(f, end):
        a = math.sin((f - 1) / (end - 1) * math.pi)
        rot("Chest", -16 * a); rot("Head", -10 * a); rot("UpperArm.L", 20 * a)
    action("Hurt", [1, 3, 7, 12], hurt)

    def death(f, end):
        t = (f - 1) / (end - 1)
        rot("Root", 82 * t)
        rig.pose.bones["Root"].location.y = -.02 * t
        rot("Thigh.L", -12 * t); rot("Shin.L", 25 * t)
        rot("UpperArm.L", -20 * t, 0, -18 * t); rot("UpperArm.R", 30 * t, 0, 18 * t)
    action("Death", [1, 8, 16, 24, 32], death)

    def interact(f, end):
        a = math.sin((f - 1) / (end - 1) * math.pi)
        rot("UpperArm.L", -65 * a); rot("Forearm.L", -35 * a)
        rot("Hand.L", 15 * a); rot("Head", 9 * a, -10 * a)
    action("Interact", [1, 8, 16, 24, 32], interact)
    rig.animation_data.action = bpy.data.actions["Idle"]
    bpy.context.scene.frame_set(1)


def weapons():
    previous = begin_asset()
    # Weapon points along Blender -Y; grip center is the attachment origin.
    box("Receiver", (0, -.04, .08), (.14, .35, .14), "Armor", .032)
    plate("UpperReceiver", (0, -.08, .17), .17, .32, .065, "Titanium")
    cylinder("PulseAccelerator", (0, -.18, .09), (0, -.49, .09), .058, "Edge")
    cylinder("MuzzleGuard", (0, -.43, .09), (0, -.52, .09), .073, "Armor")
    cylinder("MuzzleEmitter", (0, -.521, .09), (0, -.527, .09), .039, "Cyan")
    box("PistolGrip", (0, .055, -.055), (.09, .11, .19), "Rubber", .022, rotation=(math.radians(-12), 0, 0))
    box("Battery", (0, -.065, -.035), (.11, .16, .12), "Titanium", .018)
    for s in [-1, 1]:
        box("EnergyChamber", (s * .076, -.075, .09), (.014, .19, .038), "Cyan", .005)
        for i in range(4):
            box("HeatVent", (s * .078, -.18 + i * .04, .145), (.02, .018, .04), "Rubber", .004)
    box("ReflexSight", (0, -.03, .235), (.08, .055, .07), "Armor", .012)
    box("SightGlass", (0, -.035, .242), (.046, .009, .035), "Cyan", .004)
    end_asset("PulseBlaster", previous)

    previous = begin_asset()
    cylinder("Grip", (0, 0, -.15), (0, 0, .12), .031, "Rubber")
    for z in [-.13, -.085, -.04, .005, .05, .095]:
        ring("GripRing", (0, 0, z), .032, .004, "Titanium")
    cylinder("Pommel", (0, 0, -.18), (0, 0, -.14), .044, "Titanium")
    box("EmitterGuard", (0, 0, .15), (.27, .07, .06), "Armor", .018)
    box("GuardLight", (0, -.04, .16), (.19, .014, .022), "Cyan", .004)
    # Diamond cross section, pointed blade: actual tapered mesh rather than stretched cube.
    vertices = [(-.058, 0, .18), (0, -.018, .18), (.058, 0, .18), (0, .018, .18),
                (-.04, 0, .93), (0, -.014, .93), (.04, 0, .93), (0, .014, .93), (0, 0, 1.14)]
    faces = [(0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
             (4, 5, 8), (5, 6, 8), (6, 7, 8), (7, 4, 8), (3, 2, 1, 0)]
    mesh = bpy.data.meshes.new("EnergyBladeMesh")
    mesh.from_pydata(vertices, [], faces)
    obj = bpy.data.objects.new("EnergyBladeEdge", mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, "EnergyBladeEdge", "Cyan")
    cylinder("BladeSpine", (0, .006, .2), (0, .006, .91), .008, "White", vertices=8)
    end_asset("EnergyBlade", previous)


def guardian():
    previous = begin_asset()
    sphere("GuardianCoreHousing", (0, 0, 2.8), (1.05, .8, 1.05), "Armor", segments=32)
    cylinder("CoreSocket", (0, -.7, 2.8), (0, -.94, 2.8), .67, "Edge", vertices=32)
    cylinder("VulnerableCore", (0, -.945, 2.8), (0, -.97, 2.8), .44, "Red", vertices=32)
    ring("CoreSeal", (0, -.975, 2.8), .53, .045, "Titanium", (math.pi / 2, 0, 0))
    for i in range(6):
        a = i / 6 * math.tau
        x, z = math.cos(a), 2.8 + math.sin(a)
        plate("SegmentedCarapace_%d" % i, (x, -.15, z), .56, 1.2, .52, "Titanium")
        sphere("ArmorRivet_%d" % i, (x, -.775, z), (.055, .025, .055), "Amber")
    for s in [-1, 1]:
        plate("GuardianShoulder", (s * 1.28, .05, 2.9), .83, 1.08, .77, "Armor")
        cylinder("ShoulderActuator", (s * .86, 0, 2.85), (s * 1.4, 0, 2.85), .19, "Edge")
        cylinder("WeaponArm", (s * 1.45, 0, 2.8), (s * 1.7, -.15, 1.95), .24, "Titanium")
        sphere("CannonJoint", (s * 1.7, -.15, 1.95), (.29, .29, .29), "Rubber")
        cylinder("PulseCannon", (s * 1.7, -.12, 1.95), (s * 1.7, -.98, 1.95), .25, "Armor", vertices=20)
        cylinder("CannonEmitter", (s * 1.7, -.99, 1.95), (s * 1.7, -1.015, 1.95), .16, "Red")
        for y in [-.4, -.65, -.88]:
            ring("CannonBand", (s * 1.7, y, 1.95), .255, .025, "Edge", (math.pi / 2, 0, 0))
        cylinder("HipStrut", (s * .45, 0, 2.0), (s * .62, 0, 1.15), .2, "Titanium")
        plate("LegShield", (s * .65, -.12, 1.35), .52, .58, .72, "Armor")
        sphere("KneeActuator", (s * .65, 0, 1.0), (.22, .23, .22), "Edge")
        cylinder("ShinPiston", (s * .65, .1, 1.0), (s * .8, .1, .3), .12, "Edge")
        plate("ShinShell", (s * .78, -.08, .62), .45, .53, .62, "Titanium")
        box("GuardianFoot", (s * .83, -.15, .17), (.65, .95, .3), "Armor", .09)
        box("GuardianShinGlow", (s * .78, -.36, .65), (.055, .025, .35), "Red", .01)
    cylinder("HeadPedestal", (0, 0, 3.7), (0, 0, 4.0), .22, "Rubber")
    plate("GuardianHead", (0, -.05, 4.0), .8, .7, .4, "Armor")
    box("GuardianVisor", (0, -.415, 4.02), (.62, .04, .07), "Red", .015)
    for x in [-.28, -.14, 0, .14, .28]:
        box("HeadVent", (x, -.41, 3.91), (.055, .025, .05), "Titanium", .005)
    end_asset("Guardian", previous)


def bit():
    previous = begin_asset()
    sphere("BitShell", (0, 0, 0), (.24, .18, .18), "White")
    sphere("BitFaceGlass", (0, -.145, 0), (.19, .055, .115), "Armor")
    for x in [-.065, .065]:
        box("BitEye", (x, -.2, .018), (.037, .018, .045), "Cyan", .01)
    box("BitMouth", (0, -.2, -.046), (.055, .015, .012), "Cyan", .004)
    ring("BitOrbit", (0, 0, 0), .34, .018, "Amber", (math.radians(18), 0, 0))
    for s in [-1, 1]:
        plate("BitSideFin", (s * .265, .03, 0), .16, .25, .07, "Titanium")
        cylinder("BitThruster", (s * .16, .08, -.14), (s * .16, .08, -.21), .045, "Cyan")
    end_asset("Bit", previous)


def terminal():
    previous = begin_asset()
    box("TerminalFoot", (0, 0, .06), (.85, .65, .12), "Armor", .045)
    plate("TerminalPedestal", (0, .05, .6), .46, .4, 1.08, "Titanium")
    box("ConsoleFrame", (0, -.1, 1.24), (1.02, .18, .68), "Armor", .055,
        rotation=(math.radians(-12), 0, 0))
    box("ScreenGlass", (0, -.205, 1.24), (.87, .022, .52), "Cyan", .025,
        rotation=(math.radians(-12), 0, 0))
    for x in [-.31, -.13, .05, .23]:
        box("ScreenCircuit", (x, -.225, 1.25), (.065, .01, .26), "Titanium", .005)
    for x in [-.33, -.11, .11, .33]:
        box("ConsoleKey", (x, -.28, .89), (.13, .13, .045), "Edge", .012)
    for s in [-1, 1]:
        cylinder("PedestalConduit", (s * .2, .15, .18), (s * .2, .15, 1.0), .025, "Rubber")
        box("TerminalStatus", (s * .25, -.17, .6), (.022, .025, .6), "Cyan", .004)
    end_asset("Terminal", previous)


def architecture():
    previous = begin_asset()
    box("FloorChassis", (0, 0, -.17), (4, 4, .34), "Armor", .035)
    # Segmented plate seams, bolts and inset strips make a repeatable industrial module.
    for x in [-.99, .99]:
        for y in [-.99, .99]:
            box("DeckPanel", (x, y, .012), (1.94, 1.94, .025), "Titanium", .018)
            for dx in [-.79, .79]:
                for dy in [-.79, .79]:
                    cylinder("RecessBolt", (x + dx, y + dy, .024), (x + dx, y + dy, .033), .028, "Edge", vertices=8)
    for s in [-1, 1]:
        box("FloorSafetyLight", (s * 1.9, 0, .04), (.035, 3.5, .025), "Cyan", .006)
    end_asset("FloorPanel", previous)

    previous = begin_asset()
    box("WallStructure", (0, 0, 1.75), (4, .36, 3.5), "Armor", .035)
    for x in [-1.0, 1.0]:
        plate("WallPanel", (x, -.215, 1.65), 1.84, .09, 2.8, "Titanium")
        box("InsetPanel", (x, -.277, 1.65), (1.5, .035, 2.35), "Armor", .045)
        for z in [.67, .89, 1.11]:
            box("WallVent", (x, -.31, z), (1.04, .035, .07), "Rubber", .012)
        box("WallChannel", (x, -.31, 2.28), (.05, .025, .68), "Cyan", .008)
    for x in [-1.88, 0, 1.88]:
        box("WallColumn", (x, -.29, 1.75), (.17, .23, 3.5), "Edge", .025)
    end_asset("WallPanel", previous)

    previous = begin_asset()
    box("ColumnFoot", (0, 0, .1), (.85, .85, .2), "Armor", .055)
    box("ColumnSpine", (0, 0, 1.8), (.45, .45, 3.4), "Titanium", .045)
    box("ColumnCapital", (0, 0, 3.45), (.85, .85, .25), "Armor", .055)
    for x in [-.15, .15]:
        box("ColumnLight", (x, -.24, 1.8), (.035, .025, 2.65), "Cyan", .006)
    for z in [.55, 1.75, 2.95]:
        box("ColumnBrace", (0, 0, z), (.58, .58, .11), "Armor", .025)
    end_asset("Pillar", previous)

    previous = begin_asset()
    for s in [-1, 1]:
        box("DoorPillar", (s * 1.45, 0, 1.6), (.5, .65, 3.2), "Titanium", .045)
        box("PillarInset", (s * 1.45, -.34, 1.6), (.18, .03, 2.4), "Armor", .01)
        box("AccessStrip", (s * 1.45, -.365, 1.6), (.038, .018, 1.9), "Cyan", .005)
        plate("PillarCap", (s * 1.45, -.1, 2.95), .66, .8, .36, "Armor")
    box("DoorLintel", (0, 0, 3.2), (3.4, .7, .5), "Armor", .06)
    box("LintelLight", (0, -.36, 3.15), (1.4, .025, .06), "Amber", .008)
    end_asset("DoorFrame", previous)

    previous = begin_asset()
    for s in [-1, 1]:
        plate("DoorLeaf", (s * .62, 0, 1.5), 1.2, .2, 3.0, "Titanium")
        box("DoorInset", (s * .62, -.115, 1.5), (.98, .04, 2.55), "Armor", .035)
        box("DoorEdgeGlow", (s * .065, -.14, 1.5), (.027, .025, 2.55), "Cyan", .004)
        for z in [.48, .64, .8]:
            box("DoorLowerVent", (s * .62, -.145, z), (.62, .025, .065), "Rubber", .008)
    end_asset("SecurityDoor", previous)

    previous = begin_asset()
    box("CrateBody", (0, 0, .5), (1.1, .85, .95), "Armor", .06)
    for x in [-.49, .49]:
        box("CrateRib", (x, 0, .52), (.08, .94, 1.04), "Titanium", .02)
    box("CrateTop", (0, 0, 1.0), (1.15, .9, .09), "Edge", .025)
    box("CrateLatch", (0, -.46, .71), (.17, .045, .15), "Titanium", .012)
    box("CrateStatus", (0, -.487, .7), (.09, .012, .03), "Amber", .005)
    for z in [.26, .37, .48]:
        box("CrateGrille", (0, -.433, z), (.72, .025, .045), "Rubber", .008)
    end_asset("Crate", previous)

    previous = begin_asset()
    for x in [-.18, .18]:
        cylinder("IndustrialPipe", (x, 0, .15), (x, 0, 3), .11, "Titanium")
        for z in [.22, 1.5, 2.9]:
            ring("PipeCollar", (x, 0, z), .12, .022, "Edge")
    for z in [.3, 1.5, 2.8]:
        box("PipeBracket", (0, .11, z), (.65, .17, .09), "Armor", .02)
    end_asset("Pipe", previous)

    previous = begin_asset()
    for x in [-1.5, 0, 1.5]:
        cylinder("RailPost", (x, 0, 0), (x, 0, 1.05), .045, "Titanium")
        box("RailMount", (x, 0, .025), (.16, .16, .05), "Armor", .014)
    for z in [.5, 1.03]:
        cylinder("SafetyRail", (-1.6, 0, z), (1.6, 0, z), .045, "Edge")
    box("RailIndicator", (0, -.052, 1.03), (.5, .025, .035), "Amber", .006)
    end_asset("SafetyRail", previous)


def save_source():
    for obj in bpy.context.scene.objects:
        obj.hide_set(False)
    # Source collection holds all assets at origin by design. FBX exports stay separate.
    # Blender sources stay outside Assets so opening Unity never requires Blender's importer.
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "NivelCero_OriginalArt.blend"))
    (OUT / "ArtManifest.json").write_text(json.dumps({
        "generator": "Tools/Art/generate_models.py", "blender": bpy.app.version_string,
        "units": "metres", "authoring": "Original procedural art created for Nivel Cero",
        "external_assets": [], "direction": "Blender front -Y, export forward -Z / up Y",
        "zero_animation_clips": ["Idle", "Walk", "Run", "Jump", "Fall", "Dash", "Shoot", "Reload",
                                  "Melee1", "Melee2", "Melee3", "Hurt", "Death", "Interact"],
        "loop_clips": ["Idle", "Walk", "Run", "Fall"], "assets": EXPORTED,
    }, ensure_ascii=False, indent=2), encoding="utf-8")


def validate_exports():
    report = []
    for path in sorted(OUT.glob("*.fbx")):
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete(use_global=False)
        for action in list(bpy.data.actions):
            bpy.data.actions.remove(action)
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
        meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
        points = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
        low = [min(point[i] for point in points) for i in range(3)]
        high = [max(point[i] for point in points) for i in range(3)]
        actions = [action.name for action in bpy.data.actions]
        rigs = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
        clip_checks = []
        if path.stem == "Zero":
            required = {"Idle", "Walk", "Run", "Jump", "Fall", "Dash", "Shoot", "Reload",
                        "Melee1", "Melee2", "Melee3", "Hurt", "Death", "Interact"}
            present = {name.split("|")[-1] for name in actions}
            if not required.issubset(present):
                raise RuntimeError("Missing Zero clips: " + str(required - present))
            if not rigs or len(rigs[0].data.bones) < 18:
                raise RuntimeError("Zero skeleton is incomplete")
            if len(meshes) != 1 or not meshes[0].vertex_groups:
                raise RuntimeError("Zero skinning is missing")
            if any(not vertex.groups for vertex in meshes[0].data.vertices):
                raise RuntimeError("Zero contains unweighted vertices")
            for clip in bpy.data.actions:
                rig = rigs[0]
                rig.animation_data.action = clip
                if clip.slots:
                    rig.animation_data.action_slot = clip.slots[0]
                first, last = clip.frame_range
                snapshots = []
                for sample in range(9):
                    bpy.context.scene.frame_set(int(round(first + (last - first) * sample / 8)))
                    snapshots.append([value for bone in rig.pose.bones for row in bone.matrix_basis for value in row])
                variation = max(abs(value - snapshots[0][i]) for snapshot in snapshots for i, value in enumerate(snapshot))
                if variation < .00001:
                    raise RuntimeError("Animation contains no pose variation: " + clip.name)
                clip_checks.append({"clip": clip.name, "first_frame": first, "last_frame": last,
                                    "sampled_pose_variation": variation})
        report.append({"asset": path.stem, "bytes": path.stat().st_size, "mesh_count": len(meshes),
                       "bounds_min_blender": low, "bounds_max_blender": high,
                       "actions": actions, "bones": sum(len(rig.data.bones) for rig in rigs),
                       "clip_checks": clip_checks})
    (OUT / "ImportValidation.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("NIVEL_CERO_FBX_VALIDATION_OK " + json.dumps(report))


def render_inspection():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE / "NivelCero_OriginalArt.blend"))
    keep = {"Zero": ((-1.9, 0, 0), 1), "Guardian": ((1.15, .7, 0), 1),
            "Bit": ((-1.95, 0, 2.45), 1.3), "PulseBlaster": ((-3.05, -.3, .65), 2),
            "EnergyBlade": ((-3.05, .1, 1.15), 1.2)}
    for collection in bpy.data.collections:
        if collection.name not in keep:
            for obj in collection.objects:
                obj.hide_render = True
            continue
        offset, scale = keep[collection.name]
        parent = bpy.data.objects.new("Inspection_" + collection.name, None)
        bpy.context.scene.collection.objects.link(parent)
        for obj in list(collection.objects):
            if obj.parent is None:
                world = obj.matrix_world.copy()
                obj.parent = parent
                obj.matrix_world = world
            obj.hide_render = False
            obj.hide_set(False)
        parent.location = offset
        parent.scale = (scale, scale, scale)
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.025))
    ground = bpy.context.object
    ground.data.materials.append(bpy.data.materials["NC_Armor"])
    for name, loc, energy, color, size in [
        ("Inspection_Key", (-4, -6, 7), 1700, (.7, .85, 1), 6),
        ("Inspection_Rim", (3, 4, 6), 2300, (.1, .65, 1), 5),
        ("Inspection_Fill", (5, -2, 3), 1000, (1, .35, .2), 5),
    ]:
        data = bpy.data.lights.new(name, "AREA")
        data.energy, data.color, data.shape, data.size = energy, color, "DISK", size
        obj = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(obj)
        obj.location = loc
        obj.rotation_euler = (Vector((0, 0, 1.8)) - obj.location).to_track_quat("-Z", "Y").to_euler()
    data = bpy.data.cameras.new("Inspection_Camera")
    camera = bpy.data.objects.new("Inspection_Camera", data)
    bpy.context.scene.collection.objects.link(camera)
    camera.location = (7, -13, 6)
    camera.rotation_euler = (Vector((-.3, 0, 1.8)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    data.type, data.ortho_scale = "ORTHO", 9.3
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1600, 1100, 100
    scene.world.color = (.025, .025, .025)
    scene.view_settings.view_transform = "AgX"
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUT / "ArtInspection.png")
    scene.frame_set(1)
    bpy.ops.render.render(write_still=True)
    print("NIVEL_CERO_INSPECTION_RENDER_OK " + scene.render.filepath)


if "--validate" in sys.argv:
    validate_exports()
elif "--render-inspection" in sys.argv:
    render_inspection()
else:
    setup()
    zero()
    weapons()
    guardian()
    bit()
    terminal()
    architecture()
    save_source()
    print("NIVEL_CERO_ART_OK " + json.dumps(EXPORTED))
