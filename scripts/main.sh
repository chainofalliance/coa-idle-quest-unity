#!/usr/bin/env bash
set -eu

START_TIME=$(date "+%s")

BASE_WORKDIR=$(pwd)

export BUILD_TARGET=WebGL
export BUILD_NAME=ChainOfAlliance-IdleGame

export BUILD_PATH=$(pwd)/Builds/$BUILD_TARGET/
mkdir -p $BUILD_PATH

LOG_FILE=$BUILD_PATH/build.log

echo "########################################"
echo "# Building ${BUILD_NAME}..."
echo "# Target:  $BUILD_TARGET"
echo "# Path:    $BUILD_PATH"
echo "# Version: $VERSION"
echo "# Logfile: $LOG_FILE"

"${UNITY_EXECUTABLE}" \
  -quit \
  -batchmode \
  -nographics \
  -projectPath . \
  -buildTarget $BUILD_TARGET \
  -customBuildTarget $BUILD_TARGET \
  -customBuildName ${BUILD_NAME} \
  -customBuildPath $BUILD_PATH \
  -executeMethod BuildCommand.PerformBuild \
  -logFile $LOG_FILE &

UNITY_PID=$!

echo -n "# "
while kill -0 $UNITY_PID &> /dev/null; do
    echo -n '.'
    sleep 2
done

wait $UNITY_PID
UNITY_EXIT_CODE=$?

echo ""
echo "#"

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "# Success!";
  echo "# You can now start another build."
else
  echo "# Failed! (Error code: $UNITY_EXIT_CODE)";
  exit 1
fi

echo "# "

CLIENT_CONTAINER=$(pwd)/scripts/client_container/
rm -rf ${BUILD_PATH}/node_modules/ &> /dev/null 
rm -rf ${CLIENT_CONTAINER}/node_modules/ &> /dev/null 
CLIENT_HOST=13.51.204.78

BUILD_PATH=${BUILD_PATH}/${BUILD_NAME}
cp $(pwd)/scripts/client_container/* ${BUILD_PATH}

scp -r ${BUILD_PATH} ubuntu@${CLIENT_HOST}:~/client/$VERSION
ssh ubuntu@${CLIENT_HOST} "sudo docker stop coa-idle-quest && sudo docker rm coa-idle-quest"
ssh ubuntu@${CLIENT_HOST} "sudo docker build -t coa-idle-quest ~/client/$VERSION"
ssh ubuntu@${CLIENT_HOST} "sudo docker run --name coa-idle-quest -p 8081:3000 -d coa-idle-quest"
ssh ubuntu@${CLIENT_HOST} "sudo docker cp coa-idle-quest:/usr/src/app/ /var/www/demo-coa-com/"

echo "#"

END_TIME=$(date "+%s")
ELAPSED_TIME=$((END_TIME-START_TIME))
ELAPSED_TIME_STR=$(date -d@${ELAPSED_TIME} -u +%H:%M:%S)
echo "# Total deploy time: ${ELAPSED_TIME_STR}"
echo "########################################"