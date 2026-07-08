import bpy

def unwrap_all_objects():
    # 모든 메쉬 오브젝트 수집
    mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']

    if not mesh_objects:
        print("[UV Unwrap] 씬에 메쉬 오브젝트가 없습니다.")
        return

    print(f"[UV Unwrap] 총 {len(mesh_objects)}개 메쉬 처리 시작")

    # Object Mode로 전환
    bpy.ops.object.mode_set(mode='OBJECT')

    success_count = 0
    fail_count    = 0

    for obj in mesh_objects:
        try:
            # ① 다른 오브젝트 선택 해제 후 현재 오브젝트만 선택
            bpy.ops.object.select_all(action='DESELECT')
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)

            # ② Edit Mode 진입
            bpy.ops.object.mode_set(mode='EDIT')

            # ③ 전체 선택
            bpy.ops.mesh.select_all(action='SELECT')

            # ④ Smart UV Project 실행
            # angle_limit  : 이 각도 이상 꺾인 면을 별도 아일랜드로 분리
            # island_margin: 아일랜드 사이 간격 (너무 작으면 잉크가 번짐)
            bpy.ops.uv.smart_project(
                angle_limit    = 66.0,
                island_margin  = 0.02,
                area_weight    = 0.0,
                correct_aspect = True,
                scale_to_bounds= False
            )

            # ⑤ Pack Islands 실행 (UV 공간 0~1을 꽉 채움)
            bpy.ops.uv.pack_islands(margin=0.02)

            # ⑥ Object Mode로 복귀
            bpy.ops.object.mode_set(mode='OBJECT')

            print(f"  [완료] {obj.name}")
            success_count += 1

        except Exception as e:
            # 실패한 오브젝트는 건너뛰고 계속 진행
            bpy.ops.object.mode_set(mode='OBJECT')
            print(f"  [실패] {obj.name} → {e}")
            fail_count += 1

    print(f"\n[UV Unwrap] 완료 → 성공: {success_count}개 / 실패: {fail_count}개")


def export_fbx():
    # 현재 블렌더 파일과 같은 폴더에 _unwrapped.fbx로 저장
    blend_path = bpy.data.filepath

    if not blend_path:
        # 저장된 파일이 없으면 바탕화면에 저장
        import os
        export_path = os.path.join(os.path.expanduser("~"), "Desktop", "Fld_Amida01_unwrapped.fbx")
    else:
        import os
        folder      = os.path.dirname(blend_path)
        name        = os.path.splitext(os.path.basename(blend_path))[0]
        export_path = os.path.join(folder, name + "_unwrapped.fbx")

    bpy.ops.export_scene.fbx(
        filepath          = export_path,
        use_selection     = False,      # 전체 오브젝트 내보내기
        mesh_smooth_type  = 'FACE',     # Unity 호환 스무딩
        use_mesh_modifiers= True,
        add_leaf_bones    = False,      # 불필요한 본 추가 방지
        path_mode         = 'AUTO'
    )

    print(f"[Export] 저장 완료 → {export_path}")


# ── 실행 ──────────────────────────────────────────
unwrap_all_objects()
export_fbx()
