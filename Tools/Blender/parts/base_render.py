import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import part_common as pc
a = pc.args()
arm = pc.load_dragon(a[1] if len(a) > 1 else "A")
pc.setup_render(a[0], view=a[2] if len(a) > 2 else "side")
