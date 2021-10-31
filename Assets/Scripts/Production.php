#!/usr/bin/php
<?php

if (count($argv) != 2) {
    echo "Usage: ${argv[0]} On|Off\nie: ${argv[0]} On\n";
    exit(1);
}

$production = strtolower($argv[1]) == "on";
$on = $production ? "on" : "off";
$off = $production ? "off" : "on";

passthru("./Debugging.php $off");
passthru("./ExtraCare.php $on");
passthru("./Log.php off"); // Log is always set to off and should be activated manually

$projectSettings = '../../ProjectSettings/ProjectSettings.asset';
if (!file_exists($projectSettings)) die("Did not find file $projectSettings\n");
// On production we want to remove the CC_DEBUG pre processing variable
echo "Changing pre processing variable CC_DEBUG\n";
$data = file_get_contents($projectSettings);
if ($production) {
    $data = str_replace('CC_DEBUG', 'CC_DEB_OFF', $data);
} else {
    $data = str_replace('CC_DEB_OFF', 'CC_DEBUG', $data);
}
file_put_contents($projectSettings, $data);
