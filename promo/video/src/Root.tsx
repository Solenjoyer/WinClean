import React from 'react';
import {Composition} from 'remotion';
import {Launch} from './Launch';
import {FPS, TOTAL_FRAMES} from './timeline';

export const Root: React.FC = () => (
  <Composition
    id="WinCleanLaunch"
    component={Launch}
    durationInFrames={TOTAL_FRAMES}
    fps={FPS}
    width={1920}
    height={1080}
  />
);
