#!/bin/bash
# Rebuilds CampusConnect-Werbevideo.mp4 end to end from the committed sources.
#
# One-time setup:
#   cd tools && npm install playwright && npx playwright install chromium && cd ..
#
# Then just run:  bash make.sh
#
# Inputs (committed):  scene.html, intro-scene.html, cta-anim.html,
#                      build.sh, tools/record.js, app-screenshots/*
# Output:             CampusConnect-Werbevideo.mp4
set -e
cd "$(dirname "$0")"
PORT=8099

# 1. serve this folder so the headless browser can load the HTML scenes
python3 -m http.server "$PORT" >/dev/null 2>&1 &
SRV=$!
trap 'kill $SRV 2>/dev/null' EXIT
sleep 1

# 2. record each animated scene to clips/rec-*.webm
( cd tools && node record.js )

# 3. cut each recording to its exact scene duration -> seg/seg-*.mp4
mkdir -p seg
conv(){ ffmpeg -y -loglevel error -i "clips/rec-$1.webm" -t "$2" -r 30 \
  -vf "scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:white,setsar=1,fps=30,format=yuv420p" \
  -c:v libx264 -preset medium -crf 18 "seg/seg-$1.mp4"; echo "seg-$1 ($2s)"; }
conv intro 3.3
conv feed 2.9
conv mensa 2.7
conv timetable 3.3
conv grades 2.9
conv groups 4.0
conv cta 3.8

# 4. assemble with crossfades / swipes -> final mp4
bash build.sh CampusConnect-Werbevideo.mp4
echo "Done -> CampusConnect-Werbevideo.mp4"
