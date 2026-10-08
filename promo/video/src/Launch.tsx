import React from 'react';
import {AbsoluteFill, Sequence} from 'remotion';
import {Background} from './components/Background';
import {Scene} from './components/Scene';
import {Intro} from './scenes/Intro';
import {Outro} from './scenes/Outro';
import {Soundtrack} from './Soundtrack';
import {DesktopStage} from './Stage';
import {colors, fonts} from './theme';
import {INTRO, OUTRO, STAGE, STAGE_FROM} from './timeline';

export const Launch: React.FC = () => (
  <AbsoluteFill style={{fontFamily: fonts.text, color: colors.text, overflow: 'hidden'}}>
    <Background />
    <Scene from={INTRO.from} duration={INTRO.duration} name="intro">
      <Intro />
    </Scene>
    <Sequence from={STAGE_FROM} durationInFrames={STAGE.end} name="desktop">
      <DesktopStage />
    </Sequence>
    <Scene from={OUTRO.from} duration={OUTRO.duration} name="outro">
      <Outro />
    </Scene>
    <Soundtrack />
  </AbsoluteFill>
);
