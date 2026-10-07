import React from 'react';
import {AbsoluteFill} from 'remotion';
import {Background} from './components/Background';
import {Scene} from './components/Scene';
import {Cleanup} from './scenes/Cleanup';
import {Health} from './scenes/Health';
import {Intro} from './scenes/Intro';
import {Monitor} from './scenes/Monitor';
import {Outro} from './scenes/Outro';
import {Processes} from './scenes/Processes';
import {Storage} from './scenes/Storage';
import {Soundtrack} from './Soundtrack';
import {colors, fonts} from './theme';

export const Launch: React.FC = () => (
  <AbsoluteFill style={{fontFamily: fonts.text, color: colors.text, overflow: 'hidden'}}>
    <Background />
    <Scene name="intro">
      <Intro />
    </Scene>
    <Scene name="monitor">
      <Monitor />
    </Scene>
    <Scene name="processes">
      <Processes />
    </Scene>
    <Scene name="storage">
      <Storage />
    </Scene>
    <Scene name="cleanup">
      <Cleanup />
    </Scene>
    <Scene name="health">
      <Health />
    </Scene>
    <Scene name="outro">
      <Outro />
    </Scene>
    <Soundtrack />
  </AbsoluteFill>
);
