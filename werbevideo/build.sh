#!/bin/bash
# Assembles the 20s CampusConnect ad from 8 pre-rendered 1920x1080/30fps segments via xfade.
# Segments expected in seg/: seg-intro seg-feed seg-mensa seg-timetable seg-grades seg-groups seg-outro seg-cta
set -e
cd "$(dirname "$0")"
OUT="${1:-CampusConnect-Werbevideo.mp4}"
T=0.3
ffmpeg -y -loglevel error \
  -i seg/seg-intro.mp4 \
  -i seg/seg-feed.mp4 \
  -i seg/seg-mensa.mp4 \
  -i seg/seg-timetable.mp4 \
  -i seg/seg-grades.mp4 \
  -i seg/seg-groups.mp4 \
  -i seg/seg-cta.mp4 \
  -filter_complex "
[0][1]xfade=transition=fade:duration=$T:offset=3.0[a];
[a][2]xfade=transition=slideleft:duration=$T:offset=5.6[b];
[b][3]xfade=transition=slideleft:duration=$T:offset=8.0[c];
[c][4]xfade=transition=slideleft:duration=$T:offset=11.0[d];
[d][5]xfade=transition=slideleft:duration=$T:offset=13.6[e];
[e][6]xfade=transition=fade:duration=$T:offset=17.3[v]
" -map "[v]" -an -c:v libx264 -preset slow -crf 19 -pix_fmt yuv420p -movflags +faststart "$OUT"
echo "BUILT: $OUT"
ffprobe -v error -show_entries format=duration -of default=nw=1:nk=1 "$OUT"
